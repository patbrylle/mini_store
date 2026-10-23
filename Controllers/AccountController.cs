using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using mini_store.ViewModels.Account;
using mini_store.Services;

namespace mini_store.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IPasswordRecoveryService _recovery;

        // ONE constructor only
        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IPasswordRecoveryService recovery)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _recovery = recovery;
        }

        // =========================================================
        // LOGIN (public)
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectByRole(
                    User.IsInRole("Admin") || User.IsInRole("Staff"),
                    User.IsInRole("Customer"));
            }

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Admin-disabled accounts (lockout set far in the future)
            var user = await _userManager.FindByEmailAsync(model.Email.Trim());

            if (user != null && await _userManager.IsLockedOutAsync(user))
            {
                // only the real owner (correct password) is told why; everyone else sees the generic message
                ModelState.AddModelError("",
                    await _userManager.CheckPasswordAsync(user, model.Password)
                        ? (AccountStatus.IsDisabled(user.LockoutEnd)
                            ? "This account has been disabled. Please contact an administrator."
                            : "This account is temporarily locked. Try again in a few minutes.")
                        : "Invalid email or password.");

                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email.Trim(), model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded && user != null)
            {
                var isStoreUser = await _userManager.IsInRoleAsync(user, "Admin") ||
                                  await _userManager.IsInRoleAsync(user, "Staff");
                var isCustomer = await _userManager.IsInRoleAsync(user, "Customer");

                if (isStoreUser || isCustomer) return RedirectByRole(isStoreUser, isCustomer);

                await _signInManager.SignOutAsync();   // logged in but has no usable role
                return RedirectToAction("AccessDenied", "Account");
            }

            // wrong password, unknown email and "just got locked" all look the same
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // =========================================================
        // PASSWORD RECOVERY (public, called from the login page)
        // =========================================================

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SendResetCode([FromForm] SendResetCodeViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(new { error = FirstError() });

            var r = await _recovery.SendCodeAsync(
                model.Email, HttpContext.Connection.RemoteIpAddress?.ToString());

            return r.Throttled
                ? StatusCode(StatusCodes.Status429TooManyRequests,
                    new { error = "Please wait a minute before asking for another code." })
                : Json(new { ok = true, demoCode = r.DemoCode });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResetWithCode([FromForm] ResetWithCodeViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(new { error = FirstError() });

            var r = await _recovery.ResetAsync(model.Email, model.Code, model.NewPassword);
            return r.Ok ? Json(new { ok = true }) : BadRequest(new { error = r.Error });
        }

        // =========================================================
        // CHANGE PASSWORD (moved to the shared Profile page)
        // =========================================================

        // Keeps old bookmarks / links working
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return RedirectToAction("Index", "Profile");
        }

        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }

        // =========================================================
        // ACCESS DENIED (public, used by Program.cs)
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private IActionResult RedirectByRole(bool isStoreUser, bool isCustomer)
        {
            if (isStoreUser)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            if (isCustomer)
            {
                return RedirectToAction("Index", "CustomerPortal");
            }

            return RedirectToAction("AccessDenied", "Account");
        }

        private string FirstError() =>
            ModelState.Values.SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrEmpty(m))
            ?? "Please check what you entered.";
    }
}