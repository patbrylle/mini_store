using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using mini_store.Data;

namespace mini_store.Services
{
    public class EmailOptions
    {
        public string? Host { get; set; }
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string? User { get; set; }
        public string? Password { get; set; }
        public string? From { get; set; }
    }

    public interface IAppEmailSender
    {
        bool IsConfigured { get; }
        Task SendAsync(string to, string subject, string htmlBody);
    }

    public class SmtpEmailSender : IAppEmailSender
    {
        private readonly EmailOptions _o;
        public SmtpEmailSender(IOptions<EmailOptions> o) => _o = o.Value;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_o.Host) && !string.IsNullOrWhiteSpace(_o.From);

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            using var client = new SmtpClient(_o.Host, _o.Port)
            {
                EnableSsl = _o.EnableSsl,
                Credentials = string.IsNullOrEmpty(_o.User) ? null : new NetworkCredential(_o.User, _o.Password)
            };
            using var msg = new MailMessage(_o.From!, to, subject, htmlBody) { IsBodyHtml = true };
            await client.SendMailAsync(msg);
        }
    }

    public record RecoveryStart(bool Throttled, string? DemoCode);
    public record RecoveryResult(bool Ok, string? Error);

    public interface IPasswordRecoveryService
    {
        Task<RecoveryStart> SendCodeAsync(string email, string? ip);
        Task<RecoveryResult> ResetAsync(string email, string code, string newPassword);
    }

    // 6-digit code from Identity's own email token provider: tied to the security stamp,
    // short-lived, and dead the moment the password changes. Nothing is stored in the database.
   public class PasswordRecoveryService : IPasswordRecoveryService
{
    private const string Purpose = "ForgotPassword";
    private const int MaxAttempts = 5;
    private const int MaxSendsPerEmailHour = 5;
    private const int MaxSendsPerIpHour = 20;
    private static readonly TimeSpan ResendGap = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(15);

    private readonly UserManager<IdentityUser> _users;
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notify;
    private readonly IAppEmailSender _email;
    private readonly IMemoryCache _cache;
    private readonly IHostEnvironment _env;
    private readonly ILogger<PasswordRecoveryService> _log;

    public PasswordRecoveryService(
        UserManager<IdentityUser> users, ApplicationDbContext db, INotificationService notify,
        IAppEmailSender email, IMemoryCache cache, IHostEnvironment env,
        ILogger<PasswordRecoveryService> log)
    {
        _users = users; _db = db; _notify = notify; _email = email;
        _cache = cache; _env = env; _log = log;
    }

    private static string Key(string email) => email.Trim().ToLowerInvariant();

    // counts one hit atomically; returns false once the limit is passed
    private bool Hit(string key, int max, TimeSpan window)
    {
        var box = _cache.GetOrCreate(key, e =>
        {
            e.AbsoluteExpirationRelativeToNow = window;
            return new int[1];
        })!;
        return Interlocked.Increment(ref box[0]) <= max;
    }

    // gives one hit back (correct code, but the new password was too weak)
    private void Refund(string key)
    {
        if (_cache.TryGetValue(key, out int[]? box)) Interlocked.Decrement(ref box![0]);
    }

    public async Task<RecoveryStart> SendCodeAsync(string email, string? ip)
    {
        var k = Key(email);
        var gapKey = $"fp:gap:{k}";
        var dev = _env.IsDevelopment();

        if (_cache.TryGetValue(gapKey, out object? _) ||
            !Hit($"fp:send:{k}", MaxSendsPerEmailHour, TimeSpan.FromHours(1)) ||
            !Hit($"fp:ip:{ip ?? "unknown"}", MaxSendsPerIpHour, TimeSpan.FromHours(1)))
            return new RecoveryStart(true, null);

        _cache.Set(gapKey, true, ResendGap);

        // identical answer whether or not the account exists
        var user = await _users.FindByEmailAsync(email.Trim());
        if (user == null || AccountStatus.IsDisabled(user.LockoutEnd))
            return new RecoveryStart(false, null);

        var code = await _users.GenerateUserTokenAsync(user, TokenOptions.DefaultEmailProvider, Purpose);

        if (_email.IsConfigured)
        {
            try
            {
                await _email.SendAsync(user.Email!, "Your Mini Store password reset code",
                    $"<p>Your verification code is:</p><h2 style=\"letter-spacing:4px\">{code}</h2>" +
                    "<p>It expires in a few minutes. If you didn't ask for it, ignore this email.</p>");
                return new RecoveryStart(false, null);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Could not send the password reset email");
                // Development only: show the code on screen so you can keep testing
                return new RecoveryStart(false, dev ? code : null);
            }
        }

        if (dev)
            _log.LogWarning("SMTP is not configured: reset code for {Email} is {Code}", user.Email, code);

        return new RecoveryStart(false, dev ? code : null);
    }

    public async Task<RecoveryResult> ResetAsync(string email, string code, string newPassword)
    {
        var attKey = $"fp:att:{Key(email)}";

        // count the attempt BEFORE checking the code, so parallel requests can't slip past the limit
        if (!Hit(attKey, MaxAttempts, AttemptWindow))
            return new RecoveryResult(false, "Too many incorrect attempts. Please try again in 15 minutes.");

        var user = await _users.FindByEmailAsync(email.Trim());

        var valid = user != null
                    && !AccountStatus.IsDisabled(user.LockoutEnd)
                    && await _users.VerifyUserTokenAsync(user, TokenOptions.DefaultEmailProvider, Purpose, code.Trim());

        if (!valid)
            return new RecoveryResult(false, "The code is invalid or has expired.");

        var token = await _users.GeneratePasswordResetTokenAsync(user!);
        var result = await _users.ResetPasswordAsync(user!, token, newPassword);

        // a weak password is not a wrong code: give the attempt back so they can retry
        if (!result.Succeeded)
        {
            Refund(attKey);
            return new RecoveryResult(false, result.Describe());
        }

        await _users.ResetAccessFailedCountAsync(user!);
        await _users.SetLockoutEndDateAsync(user!, null);   // clears a temporary lockout (disabled users never get here)
        _cache.Remove(attKey);

        if (!await _users.IsInRoleAsync(user!, "Customer"))
        {
            _notify.Add("Password Reset", $"{user!.Email} reset their password using an email code.", "info", "/User");
            await _db.SaveChangesAsync();
        }

        return new RecoveryResult(true, null);
    }
}
}