using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Models;

namespace mini_store.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }


        // ============================================================
        // GET: Products
        // ============================================================

        public async Task<IActionResult> Index(
            int? categoryId,
            string? stockStatus,
            decimal? minPrice,
            decimal? maxPrice,
            int? productId)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .AsQueryable();


            // ========================================================
            // CATEGORY FILTER
            // ========================================================

            if (categoryId.HasValue)
            {
                products = products.Where(
                    p => p.CategoryId == categoryId.Value
                );
            }


            // ========================================================
            // STOCK FILTER
            // ========================================================

            if (!string.IsNullOrEmpty(stockStatus))
            {
                if (stockStatus == "InStock")
                {
                    products = products.Where(
                        p => p.Quantity > 10
                    );
                }
                else if (stockStatus == "LowStock")
                {
                    products = products.Where(
                        p => p.Quantity > 0 &&
                             p.Quantity <= 10
                    );
                }
                else if (stockStatus == "OutOfStock")
                {
                    products = products.Where(
                        p => p.Quantity == 0
                    );
                }
            }


            // ========================================================
            // MINIMUM PRICE
            // ========================================================

            if (minPrice.HasValue)
            {
                products = products.Where(
                    p => p.Price >= minPrice.Value
                );
            }


            // ========================================================
            // MAXIMUM PRICE
            // ========================================================

            if (maxPrice.HasValue)
            {
                products = products.Where(
                    p => p.Price <= maxPrice.Value
                );
            }


            // ========================================================
            // CATEGORY DROPDOWN
            // ========================================================

            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync(),
                "CategoryId",
                "CategoryName",
                categoryId
            );


            // ========================================================
            // KEEP FILTER VALUES
            // ========================================================

            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedStock = stockStatus;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            // Used to automatically open the Edit modal if needed
            ViewBag.OpenProductId = productId;


            return View(
                await products.ToListAsync()
            );
        }


        // ============================================================
        // POST: Products/Create
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index));
            }


            _context.Products.Add(product);

            await _context.SaveChangesAsync();


            // ========================================================
            // CREATE STOCK NOTIFICATION
            // ========================================================

            if (product.Quantity <= 0)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Out of Stock",

                    Message =
                        $"{product.ProductName} is out of stock.",

                    Type = "danger",

                    CreatedAt = DateTime.Now,

                    IsRead = false,

                    Link = Url.Action(
                        "Index",
                        "Products",
                        new
                        {
                            productId = product.ProductId
                        }
                    ) ?? "#"
                });
            }
            else if (product.Quantity <= 10)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Low Stock",

                    Message =
                        $"{product.ProductName} has only " +
                        $"{product.Quantity} items remaining.",

                    Type = "warning",

                    CreatedAt = DateTime.Now,

                    IsRead = false,

                    Link = Url.Action(
                        "Index",
                        "Products",
                        new
                        {
                            productId = product.ProductId
                        }
                    ) ?? "#"
                });
            }


            await _context.SaveChangesAsync();


            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // POST: Products/Edit
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Product product)
        {
            // ========================================================
            // CHECK PRODUCT ID
            // ========================================================

            if (id != product.ProductId)
            {
                return NotFound();
            }


            // ========================================================
            // CHECK VALIDATION
            // ========================================================

            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index));
            }


            // ========================================================
            // LOAD EXISTING PRODUCT
            // ========================================================

            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(
                    p => p.ProductId == id
                );


            if (existingProduct == null)
            {
                return NotFound();
            }


            // ========================================================
            // DETERMINE OLD STOCK STATUS
            // ========================================================

            string oldStatus;

            if (existingProduct.Quantity <= 0)
            {
                oldStatus = "OutOfStock";
            }
            else if (existingProduct.Quantity <= 10)
            {
                oldStatus = "LowStock";
            }
            else
            {
                oldStatus = "InStock";
            }


            // ========================================================
            // DETERMINE NEW STOCK STATUS
            // ========================================================

            string newStatus;

            if (product.Quantity <= 0)
            {
                newStatus = "OutOfStock";
            }
            else if (product.Quantity <= 10)
            {
                newStatus = "LowStock";
            }
            else
            {
                newStatus = "InStock";
            }


            // ========================================================
            // UPDATE ONLY THE FIELDS WE WANT
            // ========================================================

            existingProduct.ProductName =
                product.ProductName;

            existingProduct.CategoryId =
                product.CategoryId;

            existingProduct.Price =
                product.Price;

            existingProduct.Quantity =
                product.Quantity;


            // ========================================================
            // SAVE PRODUCT
            // ========================================================

            await _context.SaveChangesAsync();


            // ========================================================
            // CREATE STOCK NOTIFICATION
            // ONLY WHEN STOCK STATUS CHANGES
            // ========================================================

            if (oldStatus != newStatus)
            {
                if (newStatus == "OutOfStock")
                {
                    _context.Notifications.Add(
                        new Notification
                        {
                            Title = "Out of Stock",

                            Message =
                                $"{existingProduct.ProductName} " +
                                $"is out of stock.",

                            Type = "danger",

                            CreatedAt = DateTime.Now,

                            IsRead = false,

                            Link = Url.Action(
                                "Index",
                                "Products",
                                new
                                {
                                    productId =
                                        existingProduct.ProductId
                                }
                            ) ?? "#"
                        }
                    );
                }
                else if (newStatus == "LowStock")
                {
                    _context.Notifications.Add(
                        new Notification
                        {
                            Title = "Low Stock",

                            Message =
                                $"{existingProduct.ProductName} " +
                                $"has only " +
                                $"{existingProduct.Quantity} " +
                                $"items remaining.",

                            Type = "warning",

                            CreatedAt = DateTime.Now,

                            IsRead = false,

                            Link = Url.Action(
                                "Index",
                                "Products",
                                new
                                {
                                    productId =
                                        existingProduct.ProductId
                                }
                            ) ?? "#"
                        }
                    );
                }
                else if (
                    newStatus == "InStock" &&
                    oldStatus != "InStock")
                {
                    _context.Notifications.Add(
                        new Notification
                        {
                            Title = "Product Back in Stock",

                            Message =
                                $"{existingProduct.ProductName} " +
                                $"is back in stock with " +
                                $"{existingProduct.Quantity} " +
                                $"items.",

                            Type = "success",

                            CreatedAt = DateTime.Now,

                            IsRead = false,

                            Link = Url.Action(
                                "Index",
                                "Products",
                                new
                                {
                                    productId =
                                        existingProduct.ProductId
                                }
                            ) ?? "#"
                        }
                    );
                }


                await _context.SaveChangesAsync();
            }


            // ========================================================
            // RELOAD PRODUCTS PAGE
            // ========================================================

            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // POST: Products/Delete
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .FindAsync(id);


            if (product != null)
            {
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();
            }


            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // LOAD CATEGORIES
        // ============================================================

        private async Task LoadCategories(
            int? selectedCategory = null)
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