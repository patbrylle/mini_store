using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Services;
using mini_store.Validation;
using mini_store.ViewModels.Account;

namespace mini_store.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IAccountService _accounts;
        private readonly INotificationService _notify;

        public UserController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            IAccountService accounts,
            INotificationService notify)
        {
            _userManager = userManager;
            _context = context;
            _accounts = accounts;
            _notify = notify;
        }

        private string UsersLink => Url.Action("Index", "User") ?? "/User";

        // =========================================================
        // LIST (3 queries in total, not one per user)
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync();

            var roleByUser = (await (
                    from ur in _context.UserRoles
                    join r in _context.Roles on ur.RoleId equals r.Id
                    select new { ur.UserId, r.Name }).ToListAsync())
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name!).OrderBy(n => n).First()); // Admin sorts first

            var customerNames = await _context.Customers.AsNoTracking()
                .Where(c => c.UserId != null)
                .ToDictionaryAsync(c => c.UserId!, c => c.CustomerName);

            var model = users.Select(u =>
            {
                var email = u.Email ?? u.UserName ?? "";
                return new UserListViewModel
                {
                    Id = u.Id,
                    Email = email,
                    DisplayName = customerNames.TryGetValue(u.Id, out var n) ? n : email,
                    Role = roleByUser.TryGetValue(u.Id, out var role) ? role : "No Role",
                    Status = AccountStatus.IsDisabled(u.LockoutEnd) ? "disabled"
                           : AccountStatus.IsLocked(u.LockoutEnd) ? "locked" : "active"
                };
            })
            .OrderBy(u => u.Role == "Admin" ? 0 : u.Role == "Staff" ? 1 : 2)
            .ThenBy(u => u.DisplayName)
            .ToList();

            return View(model);
        }

        // =========================================================
        // CREATE STAFF
        // =========================================================

        [HttpGet]
        public IActionResult CreateStaff() => View();

        [HttpPost]
        public async Task<IActionResult> CreateStaff(CreateStaffViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = InputRules.NormalizeEmail(model.Email);
            var result = await _accounts.CreateLoginAsync(email, model.Password, "Staff");

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(model);
            }

            _notify.Add("New Staff Account", $"Staff account {email} was created successfully.", "info", UsersLink);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Staff account created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // CREATE CUSTOMER ACCOUNT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CreateCustomerAccount()
        {
            await LoadAvailableCustomersAsync();
            return View(new CreateCustomerAccountViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomerAccount(CreateCustomerAccountViewModel model)
        {
            async Task<IActionResult> Back(string? error = null)
            {
                if (error != null) ModelState.AddModelError("", error);
                await LoadAvailableCustomersAsync();
                return View(model);
            }

            if (!ModelState.IsValid) return await Back();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == model.CustomerId);

            if (customer == null) return await Back("Customer not found.");
            if (string.IsNullOrWhiteSpace(customer.Email))
                return await Back("This customer needs an email address before an account can be created.");
            if (!string.IsNullOrEmpty(customer.UserId))
                return await Back("This customer already has a linked account.");
            if (await _userManager.FindByEmailAsync(customer.Email) != null)
                return await Back("An account already uses this email address. Check the existing account before continuing.");

            var result = await _accounts.CreateLoginAsync(customer.Email, model.Password, "Customer", customer);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return await Back();
            }

            _notify.Add("Customer Account Created", $"A login account was created for {customer.CustomerName}.", "info", UsersLink);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Customer login account created and linked successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadAvailableCustomersAsync()
        {
            var customers = await _context.Customers
                .Where(c => c.UserId == null && c.Email != null && c.Email.Trim() != "")
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName");
        }

        // =========================================================
        // DISABLE / ENABLE / UNLOCK  (Staff and Customer logins)
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> SetStatus(string id, bool enable)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Admin accounts are protected.";
                return RedirectToAction(nameof(Index));
            }

            var who = user.Email ?? user.UserName;

            if (enable)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);

                _notify.Add("Account Enabled", $"Account {who} has been enabled.", "success", UsersLink);
                TempData["Success"] = "Account enabled successfully.";
            }
            else
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, AccountStatus.DisabledUntil);
                await _userManager.UpdateSecurityStampAsync(user);   // kicks out the live session

                _notify.Add("Account Disabled", $"Account {who} has been disabled.", "danger", UsersLink);
                TempData["Success"] = "Account disabled successfully.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE (Staff and Customer logins)
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Admin accounts are protected and cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            if ((await _userManager.GetUserAsync(User))?.Id == user.Id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            var isCustomer = await _userManager.IsInRoleAsync(user, "Customer");
            var who = isCustomer
                ? await _context.Customers.Where(c => c.UserId == id).Select(c => c.CustomerName).FirstOrDefaultAsync()
                : null;
            who ??= user.Email ?? user.UserName;

            var result = await _accounts.DeleteLoginAsync(id);

            if (!result.Succeeded)
            {
                TempData["Error"] = result.Describe();
                return RedirectToAction(nameof(Index));
            }

            _notify.Add(isCustomer ? "Customer Login Deleted" : "Staff Account Deleted",
                        $"The login for {who} was deleted.", "danger", UsersLink);
            await _context.SaveChangesAsync();

            TempData["Success"] = isCustomer
                ? "Customer login deleted. Their customer record was kept."
                : "Staff account deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}