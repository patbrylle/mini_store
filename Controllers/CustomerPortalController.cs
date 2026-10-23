using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Models;
using mini_store.ViewModels.CustomerPortal;

namespace mini_store.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerPortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CustomerPortalController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var customer = await GetCurrentCustomerAsync();

            if (customer == null)
                return View("NotLinked");

            var sales = await LoadSalesAsync(customer.CustomerId);
            var utangs = await LoadUtangsAsync(customer.CustomerId);

            return View(BuildModel(customer, sales, utangs));
        }

        public async Task<IActionResult> Purchases()
        {
            var customer = await GetCurrentCustomerAsync();

            if (customer == null)
                return View("NotLinked");

            var sales = await LoadSalesAsync(customer.CustomerId);

            return View(BuildModel(customer, sales, new()));
        }

        public async Task<IActionResult> Utang()
        {
            var customer = await GetCurrentCustomerAsync();

            if (customer == null)
                return View("NotLinked");

            var utangs = await LoadUtangsAsync(customer.CustomerId);
            

            return View(BuildModel(customer, new(), utangs));
        }

        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            return View(products);
        }

        // =========================================================
        // HELPERS
        // =========================================================

        // The customer record linked to the logged-in login (null = not linked)
        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return null;

            return await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == user.Id);
        }

        private Task<List<Sale>> LoadSalesAsync(int customerId)
        {
            return _context.Sales
                .AsNoTracking()
                .Where(s => s.CustomerId == customerId)
                .Include(s => s.SaleItems)
                    .ThenInclude(item => item.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }

        // fully qualified: the action above is also called "Utang"
        private Task<List<mini_store.Models.Utang>> LoadUtangsAsync(int customerId)
        {
            return _context.Utangs
                .AsNoTracking()
                .Where(u => u.CustomerId == customerId)
                .Include(u => u.Sale)

                .OrderByDescending(u => u.DateCreated)
                .ToListAsync();
        }

        private static CustomerPortalViewModel BuildModel(
            Customer customer,
            List<Sale> sales,
            List<mini_store.Models.Utang> utangs)
        {
            return new CustomerPortalViewModel
            {
                CustomerName = customer.CustomerName,
                TotalPurchases = sales.Sum(s => s.TotalAmount),
                TotalAmountPaid = sales.Sum(s => s.AmountPaid),
                TotalUtangBalance = utangs.Sum(u => u.Balance),
                Transactions = sales,
                Utangs = utangs
            };
        }
    }
}