using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Models;

namespace mini_store.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: /Sales
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var sales = await GetSales();

            await LoadTransactionData();

            var utangs = await _context.Utangs
                .Include(u => u.Customer)
                .Include(u => u.Sale)
                    .ThenInclude(s => s!.SaleItems)
                        .ThenInclude(si => si.Product)
                .ToListAsync();

            ViewBag.UtangsBySaleId = utangs
                .Where(u => u.SaleId > 0)
                .GroupBy(u => u.SaleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(u => u.UtangId).First()
                );

            return View(sales);
        }

        // =========================================================
        // GET: /Sales/Details/5
        // =========================================================

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

        // =========================================================
        // POST: /Sales/Create
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string? TransactionType,
            int? CustomerId,
            int[]? ProductId,
            int[]? Quantity,
            decimal AmountPaid,
            DateTime? DueDate,
            string? ReturnTo)
        {
            bool returnToUtang =
                string.Equals(
                    ReturnTo,
                    "Utang",
                    StringComparison.OrdinalIgnoreCase
                );

            // =====================================================
            // BASIC VALIDATION
            // =====================================================

            if (string.IsNullOrWhiteSpace(TransactionType))
            {
                ModelState.AddModelError(
                    "",
                    "Please select a transaction type."
                );
            }

            if (TransactionType != "Cash" &&
                TransactionType != "Utang")
            {
                ModelState.AddModelError(
                    "",
                    "Invalid transaction type."
                );
            }

            if (ProductId == null ||
                ProductId.Length == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please add at least one product."
                );
            }

            if (ProductId != null &&
                Quantity == null)
            {
                ModelState.AddModelError(
                    "",
                    "Product quantity is required."
                );
            }

            if (ProductId != null &&
                Quantity != null &&
                ProductId.Length != Quantity.Length)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid product quantity."
                );
            }

            // =====================================================
            // UTANG VALIDATION
            // =====================================================

            if (TransactionType == "Utang" &&
                CustomerId == null)
            {
                ModelState.AddModelError(
                    "",
                    "Please select a customer for utang."
                );
            }

            if (TransactionType == "Utang" &&
                DueDate.HasValue &&
                DueDate.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    "",
                    "Due date cannot be in the past."
                );
            }

            // =====================================================
            // AMOUNT VALIDATION
            // =====================================================

            if (AmountPaid < 0)
            {
                ModelState.AddModelError(
                    "",
                    "Amount paid cannot be negative."
                );
            }

            // =====================================================
            // RETURN IF VALIDATION FAILED
            // =====================================================

            if (!ModelState.IsValid)
            {
                if (returnToUtang)
                {
                    TempData["TransactionError"] = string.Join(
                        " ",
                        ModelState.Values
                            .SelectMany(value => value.Errors)
                            .Select(error => error.ErrorMessage)
                            .Where(message =>
                                !string.IsNullOrWhiteSpace(message))
                    );

                    return RedirectToAction(
                        "Index",
                        "Utang"
                    );
                }

                await LoadTransactionData();

                var sales = await GetSales();

                return View("Index", sales);
            }

            // =====================================================
            // EXTRA SAFETY CHECK
            // =====================================================

            if (ProductId == null ||
                Quantity == null)
            {
                if (returnToUtang)
                {
                    TempData["TransactionError"] =
                        "Please add at least one product.";

                    return RedirectToAction(
                        "Index",
                        "Utang"
                    );
                }

                await LoadTransactionData();

                var sales = await GetSales();

                return View("Index", sales);
            }

            // =====================================================
            // DATABASE TRANSACTION
            // =====================================================

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                decimal totalAmount = 0;

                DateTime transactionTime =
                    DateTime.Now;

                // =================================================
                // CALCULATE TOTAL
                // =================================================

                for (int i = 0; i < ProductId.Length; i++)
                {
                    int productId = ProductId[i];
                    int quantity = Quantity[i];

                    if (quantity <= 0)
                    {
                        throw new Exception(
                            "Product quantity must be greater than zero."
                        );
                    }

                    var product = await _context.Products
                        .FirstOrDefaultAsync(
                            p => p.ProductId == productId
                        );

                    if (product == null)
                    {
                        throw new Exception(
                            "One of the selected products could not be found."
                        );
                    }

                    if (quantity > product.Quantity)
                    {
                        throw new Exception(
                            $"Not enough stock for {product.ProductName}. " +
                            $"Only {product.Quantity} remaining."
                        );
                    }

                    totalAmount +=
                        product.Price * quantity;
                }

                // =================================================
                // CASH SALE
                // =================================================

                if (TransactionType == "Cash")
                {
                    if (AmountPaid < totalAmount)
                    {
                        throw new Exception(
                            "Amount paid must cover the total amount."
                        );
                    }

                    var sale = new Sale
                    {
                        CustomerId = CustomerId,
                        SaleDate = transactionTime,
                        PaymentMethod = "Cash",
                        Status = "Paid",
                        AmountPaid = AmountPaid,
                        TotalAmount = totalAmount
                    };

                    _context.Sales.Add(sale);

                    // Get SaleId
                    await _context.SaveChangesAsync();

                    // ---------------------------------------------
                    // CREATE SALE ITEMS
                    // ---------------------------------------------

                    for (int i = 0; i < ProductId.Length; i++)
                    {
                        int productId = ProductId[i];
                        int quantity = Quantity[i];

                        var product = await _context.Products
                            .FirstOrDefaultAsync(
                                p => p.ProductId == productId
                            );

                        if (product == null)
                        {
                            throw new Exception(
                                "One of the selected products could not be found."
                            );
                        }

                        decimal subtotal =
                            product.Price * quantity;

                        var saleItem = new SaleItem
                        {
                            SaleId = sale.SaleId,
                            ProductId = product.ProductId,
                            Quantity = quantity,
                            UnitPrice = product.Price,
                            Subtotal = subtotal,
                            AddedAt = transactionTime
                        };

                        _context.SaleItems.Add(saleItem);

                        int oldQuantity =
                            product.Quantity;

                        product.Quantity -= quantity;

                        CreateStockNotification(
                            product,
                            oldQuantity
                        );
                    }
                }

                // =================================================
                // UTANG SALE
                // =================================================

                if (TransactionType == "Utang")
                {
                    if (CustomerId == null)
                    {
                        throw new Exception(
                            "A customer is required for utang."
                        );
                    }

                    if (AmountPaid > totalAmount)
                    {
                        throw new Exception(
                            "Amount paid cannot be greater than " +
                            "the total amount for utang."
                        );
                    }

                    // ---------------------------------------------
                    // FIND EXISTING ACTIVE UTANG
                    // ---------------------------------------------

                    var today =
                        transactionTime.Date;

                    var tomorrow =
                        today.AddDays(1);

                    var existingUtang =
                        await _context.Utangs
                            .Include(u => u.Sale)
                                .ThenInclude(s => s!.SaleItems)
                            .Where(u =>
                                u.CustomerId == CustomerId.Value &&
                                u.Balance > 0 &&
                                u.Sale != null &&
                                u.Sale.SaleDate >= today &&
                                u.Sale.SaleDate < tomorrow
                            )
                            .OrderByDescending(
                                u => u.UtangId
                            )
                            .FirstOrDefaultAsync();

                    // =============================================
                    // EXISTING UTANG
                    // =============================================

                    if (existingUtang != null &&
                        existingUtang.Sale != null)
                    {
                        var sale =
                            existingUtang.Sale;

                        // -----------------------------------------
                        // ADD PRODUCTS
                        // -----------------------------------------

                        for (int i = 0;
                             i < ProductId.Length;
                             i++)
                        {
                            int productId =
                                ProductId[i];

                            int quantity =
                                Quantity[i];

                            var product =
                                await _context.Products
                                    .FirstOrDefaultAsync(
                                        p => p.ProductId == productId
                                    );

                            if (product == null)
                            {
                                throw new Exception(
                                    "One of the selected products could not be found."
                                );
                            }

                            decimal subtotal =
                                product.Price * quantity;

                            var saleItem =
                                new SaleItem
                                {
                                    SaleId = sale.SaleId,
                                    ProductId = product.ProductId,
                                    Quantity = quantity,
                                    UnitPrice = product.Price,
                                    Subtotal = subtotal,
                                    AddedAt = transactionTime
                                };

                            _context.SaleItems.Add(
                                saleItem
                            );

                            int oldQuantity =
                                product.Quantity;

                            product.Quantity -=
                                quantity;

                            CreateStockNotification(
                                product,
                                oldQuantity
                            );
                        }

                        // -----------------------------------------
                        // UPDATE SALE
                        // -----------------------------------------

                        sale.TotalAmount +=
                            totalAmount;

                        sale.AmountPaid +=
                            AmountPaid;

                        sale.PaymentMethod =
                            "Utang";

                        decimal newBalance =
                            sale.TotalAmount -
                            sale.AmountPaid;

                        if (newBalance <= 0)
                        {
                            newBalance = 0;
                            sale.Status = "Paid";
                        }
                        else if (sale.AmountPaid > 0)
                        {
                            sale.Status = "Partial";
                        }
                        else
                        {
                            sale.Status = "Unpaid";
                        }

                        // -----------------------------------------
                        // UPDATE UTANG
                        // -----------------------------------------

                        existingUtang.Amount +=
                            totalAmount;

                        existingUtang.AmountPaid +=
                            AmountPaid;

                        existingUtang.Balance =
                            Math.Max(
                                0,
                                existingUtang.Amount -
                                existingUtang.AmountPaid
                            );

                        existingUtang.Status =
                            sale.Status;

                        if (DueDate.HasValue)
                        {
                            existingUtang.DueDate =
                                DueDate;
                        }
                    }

                    // =============================================
                    // CREATE NEW UTANG
                    // =============================================

                    else
                    {
                        decimal balance =
                            totalAmount -
                            AmountPaid;

                        string status =
                            AmountPaid >= totalAmount
                                ? "Paid"
                                : AmountPaid > 0
                                    ? "Partial"
                                    : "Unpaid";

                        // -----------------------------------------
                        // CREATE SALE
                        // -----------------------------------------

                        var sale = new Sale
                        {
                            CustomerId = CustomerId,
                            SaleDate = transactionTime,
                            PaymentMethod = "Utang",
                            Status = status,
                            AmountPaid = AmountPaid,
                            TotalAmount = totalAmount
                        };

                        _context.Sales.Add(sale);

                        // Get SaleId
                        await _context.SaveChangesAsync();

                        // -----------------------------------------
                        // CREATE SALE ITEMS
                        // -----------------------------------------

                        for (int i = 0;
                             i < ProductId.Length;
                             i++)
                        {
                            int productId =
                                ProductId[i];

                            int quantity =
                                Quantity[i];

                            var product =
                                await _context.Products
                                    .FirstOrDefaultAsync(
                                        p => p.ProductId == productId
                                    );

                            if (product == null)
                            {
                                throw new Exception(
                                    "One of the selected products could not be found."
                                );
                            }

                            decimal subtotal =
                                product.Price * quantity;

                            var saleItem =
                                new SaleItem
                                {
                                    SaleId = sale.SaleId,
                                    ProductId = product.ProductId,
                                    Quantity = quantity,
                                    UnitPrice = product.Price,
                                    Subtotal = subtotal,
                                    AddedAt = transactionTime
                                };

                            _context.SaleItems.Add(
                                saleItem
                            );

                            int oldQuantity =
                                product.Quantity;

                            product.Quantity -=
                                quantity;

                            CreateStockNotification(
                                product,
                                oldQuantity
                            );
                        }

                        // -----------------------------------------
                        // CREATE UTANG
                        // -----------------------------------------

                        var utang = new Utang
                        {
                            CustomerId =
                                CustomerId.Value,

                            SaleId =
                                sale.SaleId,

                            Amount =
                                totalAmount,

                            AmountPaid =
                                AmountPaid,

                            Balance =
                                balance,

                            DateCreated =
                                transactionTime,

                            DueDate =
                                DueDate,

                            Status =
                                status
                        };

                        _context.Utangs.Add(
                            utang
                        );
                    }
                }

                // =================================================
                // CREATE SALE NOTIFICATION
                // =================================================

                string customerName =
                    "Customer";

                if (CustomerId.HasValue)
                {
                    var customer =
                        await _context.Customers
                            .FirstOrDefaultAsync(
                                c => c.CustomerId ==
                                     CustomerId.Value
                            );

                    if (customer != null)
                    {
                        customerName =
                            customer.CustomerName;
                    }
                }

                var notification =
                    new Notification
                    {
                        Title =
                            TransactionType == "Utang"
                                ? "New Utang Sale"
                                : "New Sale",

                        Message =
                            TransactionType == "Utang"
                                ? $"{customerName} recorded an utang of ₱{totalAmount:N2}."
                                : $"Cash sale recorded for ₱{totalAmount:N2}.",

                        Type = "success",

                        CreatedAt =
                            transactionTime,

                        IsRead = false,

                        Link =
                            Url.Action(
                                "Index",
                                "Sales"
                            ) ?? "#"
                    };

                _context.Notifications.Add(
                    notification
                );

                // =================================================
                // SAVE EVERYTHING
                // =================================================

                await _context.SaveChangesAsync();

                // =================================================
                // THIS IS THE IMPORTANT FIX
                // COMMIT FOR BOTH CASH AND UTANG
                // =================================================

                await transaction.CommitAsync();

                // =================================================
                // RETURN
                // =================================================

                return returnToUtang
                    ? RedirectToAction(
                        "Index",
                        "Utang"
                    )
                    : RedirectToAction(
                        nameof(Index)
                    );
            }
            catch (Exception ex)
            {
                // =================================================
                // ROLLBACK
                // =================================================

                await transaction.RollbackAsync();

                if (returnToUtang)
                {
                    TempData["TransactionError"] =
                        ex.Message;

                    return RedirectToAction(
                        "Index",
                        "Utang"
                    );
                }

                ModelState.AddModelError(
                    "",
                    ex.Message
                );

                await LoadTransactionData();

                var sales =
                    await GetSales();

                return View(
                    "Index",
                    sales
                );
            }
        }

        // =========================================================
        // GET ALL SALES
        // =========================================================

        private async Task<List<Sale>> GetSales()
        {
            return await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }

        // =========================================================
        // LOAD CUSTOMERS AND PRODUCTS
        // =========================================================

        private async Task LoadTransactionData()
        {
            ViewBag.Customers =
                await _context.Customers
                    .OrderBy(c => c.CustomerName)
                    .ToListAsync();

            ViewBag.Products =
                await _context.Products
                    .Where(p => p.Quantity > 0)
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();
        }

        // =========================================================
        // STOCK NOTIFICATION
        // =========================================================

        private void CreateStockNotification(
            Product product,
            int oldQuantity)
        {
            string oldStatus;

            if (oldQuantity <= 0)
            {
                oldStatus = "OutOfStock";
            }
            else if (oldQuantity <= 10)
            {
                oldStatus = "LowStock";
            }
            else
            {
                oldStatus = "InStock";
            }

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

            // No status change = no notification.
            if (oldStatus == newStatus)
            {
                return;
            }

            if (newStatus == "OutOfStock")
            {
                _context.Notifications.Add(
                    new Notification
                    {
                        Title = "Out of Stock",

                        Message =
                            $"{product.ProductName} is out of stock.",

                        Type = "danger",

                        CreatedAt =
                            DateTime.Now,

                        IsRead = false,

                        Link =
                            Url.Action(
                                "Index",
                                "Products",
                                new
                                {
                                    productId =
                                        product.ProductId
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
                            $"{product.ProductName} has only " +
                            $"{product.Quantity} items remaining.",

                        Type = "warning",

                        CreatedAt =
                            DateTime.Now,

                        IsRead = false,

                        Link =
                            Url.Action(
                                "Index",
                                "Products",
                                new
                                {
                                    productId =
                                        product.ProductId
                                }
                            ) ?? "#"
                    }
                );
            }
        }

        // =========================================================
        // DELETE SALE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                .FirstOrDefaultAsync(
                    s => s.SaleId == id
                );

            // Only paid sales can be deleted.
            if (sale == null ||
                !string.Equals(
                    sale.Status,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    nameof(Index)
                );
            }

            // Restore stock.
            foreach (var item in sale.SaleItems)
            {
                var product =
                    await _context.Products.FindAsync(
                        item.ProductId
                    );

                if (product != null)
                {
                    product.Quantity +=
                        item.Quantity;
                }
            }

            _context.SaleItems.RemoveRange(
                sale.SaleItems
            );

            _context.Sales.Remove(
                sale
            );

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index)
            );
        }
    }
}