using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Models;

namespace mini_store.Services
{
    public static class AccountStatus
    {
        // "Disabled" = locked out practically forever. A short lockout is only failed-login protection.
        public static bool IsDisabled(DateTimeOffset? end) =>
            end.HasValue && end.Value > DateTimeOffset.UtcNow.AddYears(10);

        public static bool IsLocked(DateTimeOffset? end) =>
            end.HasValue && end.Value > DateTimeOffset.UtcNow && !IsDisabled(end);

        public static DateTimeOffset DisabledUntil => DateTimeOffset.UtcNow.AddYears(100);
    }

    public static class IdentityResultExtensions
    {
        public static string Describe(this IdentityResult r) =>
            string.Join(" ", r.Errors.Select(e => e.Description));
    }

    public interface IAccountService
    {
        Task<IdentityResult> CreateLoginAsync(string email, string password, string role, Customer? customer = null);
        Task<IdentityResult> DeleteLoginAsync(string userId);
        Task<IdentityResult> SyncCustomerEmailAsync(Customer customer, string? newEmail);
        Task<bool> IsEmailTakenAsync(string email, int? exceptCustomerId = null, string? exceptUserId = null);
    }

    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _users;

        public AccountService(ApplicationDbContext db, UserManager<IdentityUser> users)
        {
            _db = db;
            _users = users;
        }

        private static IdentityResult Fail(string message) =>
            IdentityResult.Failed(new IdentityError { Description = message });

        // Joins the caller's transaction if there is one; otherwise opens its own.
        private async Task<IdentityResult> InTransactionAsync(Func<Task<IdentityResult>> work)
        {
            if (_db.Database.CurrentTransaction != null) return await work();

            await using var tx = await _db.Database.BeginTransactionAsync();
            var result = await work();
            if (result.Succeeded) await tx.CommitAsync();   // not committed = rolled back on dispose
            return result;
        }

        public Task<IdentityResult> CreateLoginAsync(
            string email, string password, string role, Customer? customer = null) =>
            InTransactionAsync(async () =>
            {
                var user = new IdentityUser { UserName = email, Email = email };

                var created = await _users.CreateAsync(user, password);
                if (!created.Succeeded) return created;

                var roled = await _users.AddToRoleAsync(user, role);
                if (!roled.Succeeded) return roled;

                if (customer != null)
                {
                    customer.UserId = user.Id;
                    await _db.SaveChangesAsync();
                }

                return IdentityResult.Success;
            });

        public Task<IdentityResult> DeleteLoginAsync(string userId) =>
            InTransactionAsync(async () =>
            {
                var user = await _users.FindByIdAsync(userId);
                if (user == null) return IdentityResult.Success;

                if (await _users.IsInRoleAsync(user, "Admin"))
                    return Fail("Admin accounts are protected and cannot be deleted.");

                // unlink first, otherwise the customer keeps a dead UserId (stuck "Login" badge,
                // and never appears again in Create Customer Account)
                var linked = await _db.Customers.Where(c => c.UserId == userId).ToListAsync();
                foreach (var c in linked) c.UserId = null;
                if (linked.Count > 0) await _db.SaveChangesAsync();

                return await _users.DeleteAsync(user);
            });

        // login uses UserName == Email, so they must change together
        public async Task<IdentityResult> SyncCustomerEmailAsync(Customer customer, string? newEmail)
        {
            if (string.IsNullOrEmpty(customer.UserId) || string.IsNullOrEmpty(newEmail))
                return IdentityResult.Success;

            var user = await _users.FindByIdAsync(customer.UserId);
            if (user == null || string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
                return IdentityResult.Success;

            user.Email = newEmail;
            user.UserName = newEmail;

            var result = await _users.UpdateAsync(user);
            if (result.Succeeded) await _users.UpdateSecurityStampAsync(user);  // old session must sign in again
            return result;
        }

        public async Task<bool> IsEmailTakenAsync(string email, int? exceptCustomerId = null, string? exceptUserId = null)
        {
            var e = email.Trim().ToLower();

            var usedByCustomer = await _db.Customers.AnyAsync(c =>
                c.Email != null && c.Email.ToLower() == e &&
                (exceptCustomerId == null || c.CustomerId != exceptCustomerId));

            if (usedByCustomer) return true;

            var user = await _users.FindByEmailAsync(email.Trim());
            return user != null && user.Id != exceptUserId;
        }
    }
}