using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace mini_store.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // ==========================================
            // BASIC COUNTS
            // ==========================================

            ViewBag.TotalProducts = await _context.Products.CountAsync();

            ViewBag.TotalCategories = await _context.Categories.CountAsync();

            ViewBag.TotalCustomers = await _context.Customers.CountAsync();


            // ==========================================
            // TODAY'S SALES
            // ==========================================

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var todaySales = await _context.Sales
                .Where(s => s.SaleDate >= today &&
                            s.SaleDate < tomorrow)
                .SumAsync(s => s.TotalAmount);

            ViewBag.TodaySales = todaySales;


            // ==========================================
            // INVENTORY VALUE
            // Price × Quantity
            // ==========================================

            var inventoryValue = await _context.Products
                .SumAsync(p => p.Price * p.Quantity);

            ViewBag.TotalInventoryValue = inventoryValue;


            // ==========================================
            // LOW STOCK
            // 10 or fewer items
            // ==========================================

            var lowStockProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Quantity <= 10)
                .OrderBy(p => p.Quantity)
                .Take(5)
                .ToListAsync();

            ViewBag.LowStockCount = await _context.Products
                .CountAsync(p => p.Quantity <= 10);

            ViewBag.LowStockItems = lowStockProducts;


            // ==========================================
            // THIS MONTH'S SALES
            // ==========================================

            var startOfMonth = new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1
            );

            var startOfNextMonth = startOfMonth.AddMonths(1);

            var thisMonthSales = await _context.Sales
                .Where(s => s.SaleDate >= startOfMonth &&
                            s.SaleDate < startOfNextMonth)
                .SumAsync(s => s.TotalAmount);

            ViewBag.ThisMonthSales = thisMonthSales;


            // ==========================================
            // RECENT TRANSACTIONS
            // ==========================================

            var recentTransactions = await _context.Sales
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentTransactions = recentTransactions;


            // ==========================================
            // SALES CHART
            // ==========================================

            var salesData = await _context.Sales
                .Where(s => s.SaleDate >= startOfMonth &&
                            s.SaleDate < startOfNextMonth)
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Total = g.Sum(s => s.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var salesLabels = salesData
                .Select(x => x.Date.ToString("MMM dd"))
                .ToList();

            var salesValues = salesData
                .Select(x => x.Total)
                .ToList();

            ViewBag.SalesTrendLabels =
                JsonSerializer.Serialize(salesLabels);

            ViewBag.SalesTrendData =
                JsonSerializer.Serialize(salesValues);


            // ==========================================
            // CATEGORY BREAKDOWN
            // ==========================================

            var categoryData = await _context.Products
                .Include(p => p.Category)
                .GroupBy(p => p.Category != null
                    ? p.Category.CategoryName
                    : "Uncategorized")
                .Select(g => new
                {
                    Category = g.Key,
                    Quantity = g.Sum(p => p.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .ToListAsync();

            var categoryLabels = categoryData
                .Select(x => x.Category)
                .ToList();

            var categoryValues = categoryData
                .Select(x => x.Quantity)
                .ToList();

            ViewBag.CategoryLabels =
                JsonSerializer.Serialize(categoryLabels);

            ViewBag.CategoryData =
                JsonSerializer.Serialize(categoryValues);


            return View();
        }
    }
}