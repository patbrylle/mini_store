using mini_store.Data;
using mini_store.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

                var lockoutEnd =
                    await _userManager.GetLockoutEndDateAsync(user);

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
        public async Task<IActionResult> CreateStaff(
            CreateStaffViewModel model)
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
    await _userManager.AddToRoleAsync(
        user,
        "Staff"
    );

    // ==========================================
    // CREATE NOTIFICATION
    // ==========================================

    var notification = new Notification
    {
        Title = "New Staff Account",
        Message =
            $"Staff account {user.Email} was created successfully.",
        Type = "info",
        CreatedAt = DateTime.Now,
        IsRead = false,
        Link = Url.Action(
            "Index",
            "User"
        ) ?? "#"
    };

    _context.Notifications.Add(notification);

    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] =
        "Staff account created successfully.";

    return RedirectToAction(nameof(Index));
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

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? "",
                Role = roles.FirstOrDefault() ?? "Staff"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EditUserViewModel model)
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

            // Prevent invalid roles
            if (model.Role != "Staff" &&
                model.Role != "Admin")
            {
                ModelState.AddModelError(
                    "Role",
                    "Invalid role selected."
                );

                return View(model);
            }

            // Update email
            user.Email = model.Email;
            user.UserName = model.Email;

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description
                    );
                }

                return View(model);
            }

            // Get current roles
            var currentRoles =
                await _userManager.GetRolesAsync(user);

            // Remove existing roles
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(
                    user,
                    currentRoles
                );
            }

            // Add selected role
            await _userManager.AddToRoleAsync(
                user,
                model.Role
            );

            TempData["SuccessMessage"] =
                "User account updated successfully.";

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

            // Only Staff accounts can be disabled/enabled.
            if (!roles.Contains("Staff"))
            {
                TempData["ErrorMessage"] =
                    "Only Staff accounts can be disabled or enabled.";

                return RedirectToAction(nameof(Index));
            }

            var lockoutEnd =
                await _userManager.GetLockoutEndDateAsync(user);

            var isCurrentlyActive =
                !lockoutEnd.HasValue ||
                lockoutEnd.Value <= DateTimeOffset.UtcNow;

            if (isCurrentlyActive)
            {
                await _userManager.SetLockoutEnabledAsync(
                    user,
                    true
                );

                await _userManager.SetLockoutEndDateAsync(
                    user,
                    DateTimeOffset.MaxValue
                );

                TempData["SuccessMessage"] =
    "Staff account disabled successfully.";

var notification = new Notification
{
    Title = "Staff Account Disabled",
    Message =
        $"Staff account {user.Email} has been disabled.",
    Type = "danger",
    CreatedAt = DateTime.Now,
    IsRead = false,
    Link = Url.Action(
        "Index",
        "User"
    ) ?? "#"
};

_context.Notifications.Add(notification);

await _context.SaveChangesAsync();
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(
                    user,
                    null
                );

                TempData["SuccessMessage"] =
    "Staff account enabled successfully.";

var notification = new Notification
{
    Title = "Staff Account Enabled",
    Message =
        $"Staff account {user.Email} has been enabled.",
    Type = "success",
    CreatedAt = DateTime.Now,
    IsRead = false,
    Link = Url.Action(
        "Index",
        "User"
    ) ?? "#"
};

_context.Notifications.Add(notification);

await _context.SaveChangesAsync();
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

            // Only Staff accounts can be deleted here.
            if (!roles.Contains("Staff"))
            {
                TempData["ErrorMessage"] =
                    "Admin accounts are protected and cannot be deleted here.";

                return RedirectToAction(nameof(Index));
            }

            // Prevent deleting the currently logged-in account.
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser != null &&
                currentUser.Id == user.Id)
            {
                TempData["ErrorMessage"] =
                    "You cannot delete your own account.";

                return RedirectToAction(nameof(Index));
            }

            var result =
                await _userManager.DeleteAsync(user);

          if (result.Succeeded)
{
    var notification = new Notification
    {
        Title = "Staff Account Deleted",
        Message =
            $"Staff account {user.Email} was deleted.",
        Type = "danger",
        CreatedAt = DateTime.Now,
        IsRead = false,
        Link = Url.Action(
            "Index",
            "User"
        ) ?? "#"
    };

    _context.Notifications.Add(notification);

    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] =
        "Staff account deleted successfully.";
}
            else
            {
                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] =
                        error.Description;
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

