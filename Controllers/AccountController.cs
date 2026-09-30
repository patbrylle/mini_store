using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using mini_store.ViewModels.Account;

namespace mini_store.Controllers
{
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

        // =========================
        // LOGIN
        // =========================

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var user = await _userManager.FindByEmailAsync(model.Email);

    if (user != null)
    {
        var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

        if (lockoutEnd.HasValue &&
            lockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            ModelState.AddModelError(
                "",
                "Your account has been disabled. Please contact an administrator."
            );

            return View(model);
        }
    }

    var result = await _signInManager.PasswordSignInAsync(
        model.Email,
        model.Password,
        model.RememberMe,
        lockoutOnFailure: false
    );

    if (result.Succeeded)
    {
        return RedirectToAction("Index", "Dashboard");
    }

    ModelState.AddModelError(
        "",
        "Invalid email or password."
    );

    return View(model);
}

        // =========================
        // REGISTER
        // =========================

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(
                user,
                model.Password
            );

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Staff");
                
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description
                );
            }

            return View(model);
        }

        // =========================
        // FORGOT PASSWORD
        // =========================

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return RedirectToAction(
                    "ForgotPasswordConfirmation");
            }

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(user);

            return RedirectToAction(
                "ResetPassword",
                new
                {
                    email = user.Email,
                    token = token
                });
        }

        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // =========================
        // RESET PASSWORD
        // =========================

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(
            string email,
            string token)
        {
            if (string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(token))
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user =
                await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return RedirectToAction(
                    "ForgotPasswordConfirmation");
            }

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    model.Token,
                    model.Password
                );

            if (result.Succeeded)
            {
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description
                );
            }

            return View(model);
        }

        // =========================
        // MAKE ADMIN
        // =========================

        // TEMPORARY: Assign Admin role
        [HttpGet]
        public async Task<IActionResult> MakeAdmin(string email)
        {
            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var result =
                await _userManager.AddToRoleAsync(
                    user,
                    "Admin"
                );

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Content(
                $"{email} is now an Admin."
            );
        }
        // =========================
// CHANGE PASSWORD
// =========================

[HttpGet]
public IActionResult ChangePassword()
{
    if (!User.Identity?.IsAuthenticated ?? true)
    {
        return RedirectToAction("Login");
    }

    return View();
}

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

        TempData["SuccessMessage"] =
            "Your password has been changed successfully.";

        return RedirectToAction("Index", "Dashboard");
    }

    foreach (var error in result.Errors)
    {
        ModelState.AddModelError(
            "",
            error.Description
        );
    }

    return View(model);
}

        // =========================
        // LOGOUT
        // =========================

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Login",
                "Account"
            );
        }
    }
}
