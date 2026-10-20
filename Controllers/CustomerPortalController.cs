
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
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
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customer == null)
                return View("NotLinked");

            var transactions = await _context.Sales
                .AsNoTracking()
                .Where(s => s.CustomerId == customer.CustomerId)
                .Include(s => s.SaleItems)
                    .ThenInclude(item => item.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var utangs = await _context.Utangs
                .AsNoTracking()
                .Where(u => u.CustomerId == customer.CustomerId)
                .OrderByDescending(u => u.DateCreated)
                .ToListAsync();

            var model = new CustomerPortalViewModel
            {
                CustomerName = customer.CustomerName,
                TotalPurchases = transactions.Sum(s => s.TotalAmount),
                TotalAmountPaid = transactions.Sum(s => s.AmountPaid),
                TotalUtangBalance = utangs.Sum(u => u.Balance),
                Transactions = transactions,
                Utangs = utangs
            };

            return View(model);
        }

public async Task<IActionResult> Purchases()
{
    var user = await _userManager.GetUserAsync(User);

    if (user == null)
        return Challenge();

    var customer = await _context.Customers
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.UserId == user.Id);

    if (customer == null)
        return View("NotLinked");

    var transactions = await _context.Sales
        .AsNoTracking()
        .Where(s => s.CustomerId == customer.CustomerId)
        .Include(s => s.SaleItems)
            .ThenInclude(item => item.Product)
        .OrderByDescending(s => s.SaleDate)
        .ToListAsync();

    var model = new CustomerPortalViewModel
    {
        CustomerName = customer.CustomerName,
        Transactions = transactions,
        TotalPurchases = transactions.Sum(s => s.TotalAmount),
        TotalAmountPaid = transactions.Sum(s => s.AmountPaid)
    };

    return View(model);
}

public async Task<IActionResult> Utang()
{
    var user = await _userManager.GetUserAsync(User);

    if (user == null)
        return Challenge();

    var customer = await _context.Customers
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.UserId == user.Id);

    if (customer == null)
        return View("NotLinked");

    var utangs = await _context.Utangs
        .AsNoTracking()
        .Where(u => u.CustomerId == customer.CustomerId)
        .OrderByDescending(u => u.DateCreated)
        .ToListAsync();

    var model = new CustomerPortalViewModel
    {
        CustomerName = customer.CustomerName,
        Utangs = utangs,
        TotalUtangBalance = utangs.Sum(u => u.Balance)
    };

    return View(model);
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


    }

    
}
