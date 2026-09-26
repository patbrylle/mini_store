using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Models;

namespace mini_store.Controllers
{
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Inventory
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            return View(products);
        }
        // GET: Inventory/AddStock/5
public async Task<IActionResult> AddStock(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var product = await _context.Products
        .Include(p => p.Category)
        .FirstOrDefaultAsync(p => p.ProductId == id);

    if (product == null)
    {
        return NotFound();
    }

    return View(product);
}

// POST: Inventory/AddStock
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddStock(int id, int quantity)
{
    if (quantity <= 0)
    {
        ModelState.AddModelError("", "Quantity must be greater than zero.");
    }

    var product = await _context.Products
        .FirstOrDefaultAsync(p => p.ProductId == id);

    if (product == null)
    {
        return NotFound();
    }

    if (!ModelState.IsValid)
    {
        return View(product);
    }

    product.Quantity += quantity;

    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(Index));
}
    }
}