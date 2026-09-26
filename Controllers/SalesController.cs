using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Models;

namespace mini_store.Controllers
{
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Sales
        public async Task<IActionResult> Index()
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return View(sales);
        }

        // GET: Sales/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(s => s.SaleId == id);

            if (sale == null)
            {
                return NotFound();
            }

            return View(sale);
        }

        // GET: Sales/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = await _context.Customers
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            ViewBag.Products = await _context.Products
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            return View();
        }

        // POST: Sales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int? CustomerId,
            int[] ProductId,
            int[] Quantity)
        {
            if (ProductId == null || ProductId.Length == 0)
            {
                ModelState.AddModelError("", "Please add at least one product.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Customers = await _context.Customers
                    .OrderBy(c => c.CustomerName)
                    .ToListAsync();

                ViewBag.Products = await _context.Products
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

                return View();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var sale = new Sale
                {
                    CustomerId = CustomerId,
                    SaleDate = DateTime.Now,
                    PaymentMethod = "Cash",
                    Status = "Paid",
                    AmountPaid = 0,
                    TotalAmount = 0
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                decimal totalAmount = 0;

                for (int i = 0; i < ProductId.Length; i++)
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.ProductId == ProductId[i]);

                    if (product == null)
                    {
                        throw new Exception("Product not found.");
                    }

                    if (Quantity[i] <= 0)
                    {
                        throw new Exception("Quantity must be greater than zero.");
                    }

                    if (product.Quantity < Quantity[i])
                    {
                        throw new Exception(
                            $"Not enough stock for {product.ProductName}."
                        );
                    }

                    decimal subtotal = product.Price * Quantity[i];

                    var saleItem = new SaleItem
                    {
                        SaleId = sale.SaleId,
                        ProductId = product.ProductId,
                        Quantity = Quantity[i],
                        UnitPrice = product.Price,
                        Subtotal = subtotal
                    };

                    _context.SaleItems.Add(saleItem);

                    product.Quantity -= Quantity[i];

                    totalAmount += subtotal;
                }

                sale.TotalAmount = totalAmount;
                sale.AmountPaid = totalAmount;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError("", ex.Message);

                ViewBag.Customers = await _context.Customers
                    .OrderBy(c => c.CustomerName)
                    .ToListAsync();

                ViewBag.Products = await _context.Products
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

                return View();
            }
        }
    }
}