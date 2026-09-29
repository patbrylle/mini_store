using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Models;

namespace mini_store.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

       // GET: Products
public async Task<IActionResult> Index(
    int? categoryId,
    string? stockStatus,
    decimal? minPrice,
    decimal? maxPrice)
{
    var products = _context.Products
        .Include(p => p.Category)
        .AsQueryable();

    // CATEGORY FILTER
    if (categoryId.HasValue)
    {
        products = products.Where(p => p.CategoryId == categoryId.Value);
    }

    // STOCK FILTER
    if (!string.IsNullOrEmpty(stockStatus))
    {
        if (stockStatus == "InStock")
        {
            products = products.Where(p => p.Quantity > 10);
        }
        else if (stockStatus == "LowStock")
        {
            products = products.Where(p => p.Quantity > 0 && p.Quantity <= 10);
        }
        else if (stockStatus == "OutOfStock")
        {
            products = products.Where(p => p.Quantity == 0);
        }
    }

    // MINIMUM PRICE
    if (minPrice.HasValue)
    {
        products = products.Where(p => p.Price >= minPrice.Value);
    }

    // MAXIMUM PRICE
    if (maxPrice.HasValue)
    {
        products = products.Where(p => p.Price <= maxPrice.Value);
    }

    // CATEGORY DROPDOWN
    ViewBag.Categories = new SelectList(
        await _context.Categories.ToListAsync(),
        "CategoryId",
        "CategoryName",
        categoryId
    );

    // KEEP FILTER VALUES
    ViewBag.SelectedCategory = categoryId;
    ViewBag.SelectedStock = stockStatus;
    ViewBag.MinPrice = minPrice;
    ViewBag.MaxPrice = maxPrice;

    return View(await products.ToListAsync());
}


        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }


        // POST: Products/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(product);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }


        // POST: Products/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        // Used for the category dropdown
        private async Task LoadCategories(int? selectedCategory = null)
        {
            ViewBag.Categories = new SelectList(
                await _context.Categories.ToListAsync(),
                "CategoryId",
                "CategoryName",
                selectedCategory
            );
        }
    }
}