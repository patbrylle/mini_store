using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using mini_store.ViewModels.Account;

namespace mini_store.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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
        if (User.IsInRole("Admin") || User.IsInRole("Staff"))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (User.IsInRole("Customer"))
        {
            return RedirectToAction("Index", "CustomerPortal");
        }

        return RedirectToAction("AccessDenied", "Account");
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
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

                if (lockoutEnd.HasValue &&
                    lockoutEnd.Value > DateTimeOffset.UtcNow)
                {
                    ModelState.AddModelError(
                        "",
                        "This account is disabled or temporarily locked. " +
                        "Try again later or contact an administrator."
                    );

                    return View(model);
                }
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true   // enforces the 5-attempt rule
            );

           if (result.Succeeded)
{
    var loggedInUser = await _userManager.FindByEmailAsync(model.Email);

    if (loggedInUser != null)
    {
        if (await _userManager.IsInRoleAsync(loggedInUser, "Admin") ||
            await _userManager.IsInRoleAsync(loggedInUser, "Staff"))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (await _userManager.IsInRoleAsync(loggedInUser, "Customer"))
        {
            return RedirectToAction("Index", "CustomerPortal");
        }
    }

    await _signInManager.SignOutAsync();
    return RedirectToAction("AccessDenied", "Account");
}
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    "",
                    "Too many failed attempts. Try again later or contact an administrator."
                );

                return View(model);
            }

            // Same message for wrong email or wrong password
            ModelState.AddModelError("", "Invalid email or password.");

            return View(model);
        }

       

        // =========================================================
        // CHANGE PASSWORD (logged-in users)
        // =========================================================
[Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }
[Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword
            );

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                TempData["Success"] =
                    "Your password has been changed successfully.";

                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
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
    }
}