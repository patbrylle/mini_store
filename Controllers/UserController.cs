using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using mini_store.Data;
using mini_store.Models;
using mini_store.ViewModels.Account;

namespace mini_store.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // =========================================================
        // USER LIST
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var model = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

                var isActive =
                    !lockoutEnd.HasValue ||
                    lockoutEnd.Value <= DateTimeOffset.UtcNow;

                model.Add(new UserListViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? "",
                    Role = roles.FirstOrDefault() ?? "No Role",
                    IsActive = isActive
                });
            }

            return View(model);
        }

        // =========================================================
        // CREATE STAFF
        // =========================================================

        [HttpGet]
        public IActionResult CreateStaff()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(CreateStaffViewModel model)
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

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Staff");

                await Notify(
                    "New Staff Account",
                    $"Staff account {user.Email} was created successfully.",
                    "info");

                TempData["SuccessMessage"] = "Staff account created successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // =========================================================
        // EDIT USER
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            return View(new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? "",
                Role = roles.FirstOrDefault() ?? "Staff"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);

            if (user == null)
            {
                return NotFound();
            }

            if (model.Role != "Staff" && model.Role != "Admin")
            {
                ModelState.AddModelError("Role", "Invalid role selected.");
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            bool roleChanged = !currentRoles.Contains(model.Role) || currentRoles.Count != 1;

            // An admin cannot change their own role (prevents locking everyone out)
            var me = await _userManager.GetUserAsync(User);

            if (roleChanged && me != null && me.Id == user.Id)
            {
                ModelState.AddModelError("Role", "You cannot change your own role.");
                return View(model);
            }

            // Update email / username
            user.Email = model.Email;
            user.UserName = model.Email;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            // Update role
            if (roleChanged)
            {
                if (currentRoles.Any())
                {
                    var removeResult =
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);

                    if (!removeResult.Succeeded)
                    {
                        ModelState.AddModelError("", "Could not update the user's role.");
                        return View(model);
                    }
                }

                var addResult = await _userManager.AddToRoleAsync(user, model.Role);

                if (!addResult.Succeeded)
                {
                    // put the old roles back so the user is never left without one
                    if (currentRoles.Any())
                    {
                        await _userManager.AddToRolesAsync(user, currentRoles);
                    }

                    ModelState.AddModelError("", "Could not update the user's role.");
                    return View(model);
                }

                // Force the user to sign in again so the new role applies now
                await _userManager.UpdateSecurityStampAsync(user);
            }

            TempData["SuccessMessage"] = "User account updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // RESET PASSWORD (admin resets a Staff password)
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("Staff"))
            {
                TempData["ErrorMessage"] = "Only Staff passwords can be reset here.";
                return RedirectToAction(nameof(Index));
            }

            return View(new ResetUserPasswordViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetUserPasswordViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("Staff"))
            {
                TempData["ErrorMessage"] = "Only Staff passwords can be reset here.";
                return RedirectToAction(nameof(Index));
            }

            model.Email = user.Email ?? user.UserName ?? "";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            // Sign the user out everywhere and clear any lockout from failed attempts
            await _userManager.UpdateSecurityStampAsync(user);
            await _userManager.ResetAccessFailedCountAsync(user);

            await Notify(
                "Staff Password Reset",
                $"The password for {user.Email} was reset by an administrator.",
                "info");

            TempData["SuccessMessage"] = "Password reset successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ENABLE / DISABLE USER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("Staff"))
            {
                TempData["ErrorMessage"] =
                    "Only Staff accounts can be disabled or enabled.";
                return RedirectToAction(nameof(Index));
            }

            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

            var isCurrentlyActive =
                !lockoutEnd.HasValue ||
                lockoutEnd.Value <= DateTimeOffset.UtcNow;

            if (isCurrentlyActive)
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

                // Kick out any session that is already logged in
                await _userManager.UpdateSecurityStampAsync(user);

                await Notify(
                    "Staff Account Disabled",
                    $"Staff account {user.Email} has been disabled.",
                    "danger");

                TempData["SuccessMessage"] = "Staff account disabled successfully.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);

                await Notify(
                    "Staff Account Enabled",
                    $"Staff account {user.Email} has been enabled.",
                    "success");

                TempData["SuccessMessage"] = "Staff account enabled successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE STAFF
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("Staff"))
            {
                TempData["ErrorMessage"] =
                    "Admin accounts are protected and cannot be deleted here.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                await Notify(
                    "Staff Account Deleted",
                    $"Staff account {user.Email} was deleted.",
                    "danger");

                TempData["SuccessMessage"] = "Staff account deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    string.Join(" ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // HELPER
        // =========================================================

        private async Task Notify(string title, string message, string type)
        {
            _context.Notifications.Add(new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.Now,
                IsRead = false,
                Link = Url.Action("Index", "User") ?? "#"
            });

            await _context.SaveChangesAsync();
        }
    }
}