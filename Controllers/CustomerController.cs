using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Models;

namespace mini_store.Controllers
{
[Authorize(Roles = "Admin,Staff")]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: Customers
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers
                .AsNoTracking()
                .ToListAsync();

            return View(customers);
        }


        // =========================================================
        // POST: Customers/Create
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                var customers = await _context.Customers
                    .AsNoTracking()
                    .ToListAsync();

                return View("Index", customers);
            }

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
            {
                var customers = await _context.Customers
                    .AsNoTracking()
                    .ToListAsync();

                return View("Index", customers);
            }

            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (existingCustomer == null)
            {
                return NotFound();
            }

            // Update the existing record
            existingCustomer.CustomerName = customer.CustomerName;
            existingCustomer.Email = customer.Email;
            existingCustomer.ContactNumber = customer.ContactNumber;
            existingCustomer.Address = customer.Address;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{existingCustomer.CustomerName} was updated successfully.";

            // IMPORTANT:
            // Reload the Customers page after saving.
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

            _context.Customers.Remove(customer);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{customer.CustomerName} was deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}