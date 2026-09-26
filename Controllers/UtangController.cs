using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Models;

namespace mini_store.Controllers
{
    public class UtangController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UtangController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Utang
        public async Task<IActionResult> Index()
        {
            var utangs = await _context.Utangs
                .Include(u => u.Customer)
                .Include(u => u.Sale)
                    .ThenInclude(s => s!.SaleItems)
                        .ThenInclude(si => si.Product)
                .Where(u => u.Balance > 0)
                .OrderByDescending(u => u.DateCreated)
                .ToListAsync();

            return View(utangs);
        }

        // GET: Utang/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var utang = await _context.Utangs
                .Include(u => u.Customer)
                .Include(u => u.Sale)
                    .ThenInclude(s => s!.SaleItems)
                        .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(u => u.UtangId == id);

            if (utang == null)
            {
                return NotFound();
            }

            return View(utang);
        }
        // GET: Utang/Create
public async Task<IActionResult> Create()
{
    ViewBag.Customers = await _context.Customers
        .OrderBy(c => c.CustomerName)
        .ToListAsync();

    ViewBag.Products = await _context.Products
        .Where(p => p.Quantity > 0)
        .OrderBy(p => p.ProductName)
        .ToListAsync();

    return View(new CreateUtang());
}
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreateUtang model)
{
    var selectedProducts = model.Products
        .Where(p => p.Quantity > 0)
        .ToList();

    if (selectedProducts.Count == 0)
    {
        ModelState.AddModelError("", "Please select at least one product.");
    }

    if (!ModelState.IsValid)
    {
        ViewBag.Customers = await _context.Customers
            .OrderBy(c => c.CustomerName)
            .ToListAsync();

        ViewBag.Products = await _context.Products
            .Where(p => p.Quantity > 0)
            .OrderBy(p => p.ProductName)
            .ToListAsync();

        return View(model);
    }

    decimal totalAmount = 0;

    var sale = new Sale
    {
        CustomerId = model.CustomerId,
        SaleDate = DateTime.Now,
        PaymentMethod = "Utang",
        Status = "Unpaid",
        AmountPaid = model.AmountPaid
    };

    foreach (var item in selectedProducts)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);

        if (product == null)
            continue;

        if (item.Quantity > product.Quantity)
        {
            ModelState.AddModelError("", 
                $"Not enough stock for {product.ProductName}.");

            ViewBag.Customers = await _context.Customers
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            ViewBag.Products = await _context.Products
                .Where(p => p.Quantity > 0)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            return View(model);
        }

        decimal subtotal = product.Price * item.Quantity;

        sale.SaleItems.Add(new SaleItem
        {
            ProductId = product.ProductId,
            Quantity = item.Quantity,
            UnitPrice = product.Price,
            Subtotal = subtotal
        });

        product.Quantity -= item.Quantity;

        totalAmount += subtotal;
    }

    sale.TotalAmount = totalAmount;

    _context.Sales.Add(sale);

    var utang = new Utang
    {
        CustomerId = model.CustomerId,
        Sale = sale,
        Amount = totalAmount,
        AmountPaid = model.AmountPaid,
        Balance = totalAmount - model.AmountPaid,
        DateCreated = DateTime.Now,
        Status = model.AmountPaid >= totalAmount ? "Paid" : "Unpaid"
    };

    _context.Utangs.Add(utang);

    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(Index));
}
    }
}