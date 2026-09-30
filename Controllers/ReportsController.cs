using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using Microsoft.AspNetCore.Authorization;


namespace mini_store.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports
        public async Task<IActionResult> Index()
        {
            // SALES
            var totalSales = await _context.Sales
                .SumAsync(s => s.TotalAmount);

            var totalTransactions = await _context.Sales
                .CountAsync();

            var totalPaid = await _context.Sales
                .SumAsync(s => s.AmountPaid);

            var outstandingSales = await _context.Sales
                .SumAsync(s => s.TotalAmount - s.AmountPaid);

            // INVENTORY
            var totalProducts = await _context.Products
                .CountAsync();

            var totalStock = await _context.Products
                .SumAsync(p => p.Quantity);

            var lowStock = await _context.Products
                .CountAsync(p => p.Quantity > 0 && p.Quantity <= 10);

            var outOfStock = await _context.Products
                .CountAsync(p => p.Quantity == 0);

            // UTANG
            var totalUtang = await _context.Utangs
                .SumAsync(u => u.Amount);

            var utangPaid = await _context.Utangs
                .SumAsync(u => u.AmountPaid);

            var utangBalance = await _context.Utangs
                .SumAsync(u => u.Balance);

            // VIEWBAG
            ViewBag.TotalSales = totalSales;
            ViewBag.TotalTransactions = totalTransactions;
            ViewBag.TotalPaid = totalPaid;
            ViewBag.OutstandingSales = outstandingSales;

            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalStock = totalStock;
            ViewBag.LowStock = lowStock;
            ViewBag.OutOfStock = outOfStock;

            ViewBag.TotalUtang = totalUtang;
            ViewBag.UtangPaid = utangPaid;
            ViewBag.UtangBalance = utangBalance;

            // BEST SELLING PRODUCTS
            var bestSellingProducts = await _context.SaleItems
                .Include(si => si.Product)
                .GroupBy(si => new
                {
                    si.ProductId,
                    si.Product!.ProductName
                })
                .Select(g => new
                {
                    ProductName = g.Key.ProductName,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(10)
                .ToListAsync();

            ViewBag.BestSellingProducts = bestSellingProducts;

            return View();
        }
    }
}