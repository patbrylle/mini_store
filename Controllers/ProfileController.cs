using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.ViewModels.Account;
using mini_store.ViewModels.Profile;
using mini_store.Models;
using mini_store.Services;
using System.Security.Claims;
using mini_store.Validation;

namespace mini_store.Controllers
{
    // Shared by Admin, Staff and Customer.
    // Every action only works on the LOGGED-IN user's own account;
    // no user id or customer id is ever accepted from the form.
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IAccountService _accounts;


      public ProfileController(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    ApplicationDbContext context,
    IAccountService accounts)
{
    _userManager = userManager;
    _signInManager = signInManager;
    _context = context;
    _accounts = accounts;
}

        // =========================================================
        // VIEW PROFILE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            return View(await BuildPageAsync(user));
        }

        // =========================================================
        // UPDATE PERSONAL INFO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            [Bind(Prefix = "Info")] UpdateProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var isCustomer = User.IsInRole("Customer");

            // tracked on purpose, because we edit it below
            var customer = isCustomer
                ? await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id)
                : null;

                     model.Email = InputRules.NormalizeEmail(model.Email);
model.PhoneNumber = InputRules.NormalizePhone(model.PhoneNumber);
model.Address = InputRules.Clean(model.Address);

if (ModelState.IsValid &&
    await _accounts.IsEmailTakenAsync(model.Email, customer?.CustomerId, user.Id))
{
    ModelState.AddModelError("Info.Email", "That email address is already in use.");
}
            // Login and Customers row must change together or they drift apart
            await using var tx = await _context.Database.BeginTransactionAsync();

            // Login uses UserName, and this app always keeps UserName == Email
            user.Email = model.Email;
            user.UserName = model.Email;

            if (!isCustomer)
            {
                user.PhoneNumber = model.PhoneNumber;
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                // duplicate email / username etc.
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("Info.Email", error.Description);
                }

                return View("Index", await BuildPageAsync(user, model));
            }

                       if (customer != null)
            {
                // The NAME is deliberately not editable here: utang and sales records
                // are shown under it. Only the admin can change it (Customers page).
                var changes = new List<string>();

                void Track(string label, string? oldValue, string? newValue)
                {
                    if (!string.Equals(oldValue ?? "", newValue ?? "", StringComparison.OrdinalIgnoreCase))
                    {
                        changes.Add(
                            $"{label}: {(string.IsNullOrWhiteSpace(oldValue) ? "none" : oldValue)} → " +
                            $"{(string.IsNullOrWhiteSpace(newValue) ? "none" : newValue)}");
                    }
                }

                Track("email", customer.Email, model.Email);
                Track("contact", customer.ContactNumber, model.PhoneNumber);
                Track("address", customer.Address, model.Address);

                customer.Email = model.Email;
                customer.ContactNumber = model.PhoneNumber;
                customer.Address = model.Address;

                if (changes.Count > 0)
                {
                    _context.Notifications.Add(new Notification
                    {
                        Title = "Customer Updated Their Details",
                        Message = $"{customer.CustomerName} changed their " + string.Join("; ", changes),
                        Type = "info",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        Link = Url.Action("Index", "Customers") ?? "#"
                    });
                }

                await _context.SaveChangesAsync();
            }

            await tx.CommitAsync();

            // the cookie still holds the OLD email, so issue a fresh one
            await _signInManager.RefreshSignInAsync(user);

            TempData["Success"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // CHANGE PASSWORD
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            [Bind(Prefix = "Password")] ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            if (ModelState.IsValid && model.CurrentPassword == model.NewPassword)
            {
                ModelState.AddModelError(
                    "Password.NewPassword",
                    "Your new password must be different from your current password.");
            }

            if (!ModelState.IsValid)
            {
                return View("Index", await BuildPageAsync(user));
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    var key = error.Code == "PasswordMismatch"
                        ? "Password.CurrentPassword"
                        : "Password.NewPassword";

                    ModelState.AddModelError(key, error.Description);
                }

                return View("Index", await BuildPageAsync(user));
            }

            // password change updates the security stamp; keep THIS session alive
            await _signInManager.RefreshSignInAsync(user);

            TempData["Success"] = "Your password has been changed successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // HELPER
        // =========================================================

        private async Task<ProfilePageViewModel> BuildPageAsync(
            IdentityUser user,
            UpdateProfileViewModel? info = null)
        {
            var isCustomer = User.IsInRole("Customer");

            var customer = isCustomer
                ? await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == user.Id)
                : null;

            return new ProfilePageViewModel
            {
                RoleLabel = User.IsInRole("Admin") ? "Administrator"
                          : isCustomer ? "Customer"
                          : User.IsInRole("Staff") ? "Staff"
                          : "User",

                IsCustomer = isCustomer,
                HasCustomerRecord = customer != null,

                // User.Identity.Name = the email currently in the cookie
                DisplayName = customer?.CustomerName ?? User.Identity?.Name ?? "User",

                // after a failed post we keep what the user typed
                Info = info ?? new UpdateProfileViewModel
                {
                    FullName = customer?.CustomerName,
                    Email = user.Email ?? user.UserName ?? "",
                    PhoneNumber = customer != null ? customer.ContactNumber : user.PhoneNumber,
                    Address = customer?.Address
                }
            };
        }
    }
}