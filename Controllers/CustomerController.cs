using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Models;
using mini_store.Services;

namespace mini_store.Controllers
{
[Authorize(Roles = "Admin,Staff")]
    public class CustomersController : Controller
    {
         private readonly ApplicationDbContext _context;
private readonly UserManager<IdentityUser> _userManager;
private readonly ISalesService _sales;

public CustomersController(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager,
    ISalesService sales)
{
    _context = context;
    _userManager = userManager;
    _sales = sales;
}
        // =========================================================
        // GET: Customers
        // =========================================================
// one loader for Index and for both validation-failure paths
private async Task<List<Customer>> LoadCustomersAsync()
{
    ViewBag.Balances = await _sales.GetUtangBalancesAsync();
    ViewBag.Form = await _sales.GetFormDataAsync(); // needed by the shared transaction modal
    return await _context.Customers.AsNoTracking().ToListAsync();
}

public async Task<IActionResult> Index() => View(await LoadCustomersAsync());

// GET: Customers/History/5  (modal body, loaded by AJAX)
[HttpGet]
public async Task<IActionResult> History(int id)
{
    var vm = await _sales.GetCustomerHistoryAsync(id);
    return vm == null ? NotFound() : PartialView("_CustomerHistory", vm);
}

        // =========================================================
        // POST: Customers/Create
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CustomerName,Email,ContactNumber,Address")] Customer customer)        {
          if (!ModelState.IsValid)
    return View("Index", await LoadCustomersAsync());

            _context.Customers.Add(customer);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{customer.CustomerName} was added successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // POST: Customers/Edit
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return NotFound();
            }

          if (!ModelState.IsValid)
    return View("Index", await LoadCustomersAsync());

            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (existingCustomer == null)
            {
                return NotFound();
            }

            var newEmail = customer.Email?.Trim();
            var hasLogin = !string.IsNullOrEmpty(existingCustomer.UserId);

            // a customer with a login must always keep an email
            if (hasLogin && string.IsNullOrWhiteSpace(newEmail))
            {
                TempData["DeleteError"] =
                    "This customer has a login account, so the email address cannot be empty.";

                return RedirectToAction(nameof(Index));
            }

            // customer row + login are saved together or not at all
            await using var tx = await _context.Database.BeginTransactionAsync();

            // keep the login in sync (login uses UserName, which equals Email in this app)
            if (hasLogin &&
                !string.Equals(existingCustomer.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                var loginUser = await _userManager.FindByIdAsync(existingCustomer.UserId!);

                if (loginUser != null)
                {
                    loginUser.Email = newEmail;
                    loginUser.UserName = newEmail;

                    var result = await _userManager.UpdateAsync(loginUser);

                    if (!result.Succeeded)
                    {
                        TempData["DeleteError"] =
                            string.Join(" ", result.Errors.Select(e => e.Description));

                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            existingCustomer.CustomerName = customer.CustomerName;
            existingCustomer.Email = newEmail;
            existingCustomer.ContactNumber = customer.ContactNumber;
            existingCustomer.Address = customer.Address;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["SuccessMessage"] =
                $"{existingCustomer.CustomerName} was updated successfully.";

            return RedirectToAction(nameof(Index));
        }
        // =========================================================
        // POST: Customers/Delete
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            // Check whether this customer has existing sales.
            var hasSales = await _context.Sales
                .AnyAsync(s => s.CustomerId == id);

            if (hasSales)
            {
                TempData["DeleteError"] =
                    $"Cannot delete {customer.CustomerName} because this customer has existing sales records.";

                return RedirectToAction(nameof(Index));
            }

                      var loginUserId = customer.UserId;

            _context.Customers.Remove(customer);

            await _context.SaveChangesAsync();

            // also remove the customer's login so the email can be reused
            if (!string.IsNullOrEmpty(loginUserId))
            {
                var loginUser = await _userManager.FindByIdAsync(loginUserId);

                if (loginUser != null)
                {
                    await _userManager.DeleteAsync(loginUser);
                }
            }

            TempData["SuccessMessage"] =
                $"{customer.CustomerName} was deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}