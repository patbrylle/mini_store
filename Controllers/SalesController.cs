
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

        // =========================================================
        // GET: /Sales
        // =========================================================

        public async Task<IActionResult> Index()
{
    var sales = await GetSales();

    await LoadTransactionData();

    // Load utang records so the Sales page can open
    // the reusable Utang Details modal.
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
            string? ReturnTo)
        {
            bool returnToUtang =
                string.Equals(ReturnTo, "Utang", StringComparison.OrdinalIgnoreCase);

            // -----------------------------------------------------
            // BASIC VALIDATION
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // UTANG REQUIRES CUSTOMER
            // -----------------------------------------------------

            if (TransactionType == "Utang" &&
                CustomerId == null)
            {
                ModelState.AddModelError(
                    "",
                    "Please select a customer for utang."
                );
            }

            // -----------------------------------------------------
            // AMOUNT PAID CANNOT BE NEGATIVE
            // -----------------------------------------------------

            if (AmountPaid < 0)
            {
                ModelState.AddModelError(
                    "",
                    "Amount paid cannot be negative."
                );
            }

            // -----------------------------------------------------
            // IF VALIDATION FAILED
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                if (returnToUtang)
                {
                    TempData["TransactionError"] = string.Join(
                        " ",
                        ModelState.Values
                            .SelectMany(value => value.Errors)
                            .Select(error => error.ErrorMessage)
                            .Where(message => !string.IsNullOrWhiteSpace(message))
                    );

                    return RedirectToAction("Index", "Utang");
                }

                await LoadTransactionData();

                var sales = await GetSales();

                return View("Index", sales);
            }
            if (ProductId == null || Quantity == null)
{
    if (returnToUtang)
    {
        TempData["TransactionError"] = "Please add at least one product.";
        return RedirectToAction("Index", "Utang");
    }

    await LoadTransactionData();

    var sales = await GetSales();

    return View("Index", sales);
}

            // -----------------------------------------------------
            // DATABASE TRANSACTION
            // -----------------------------------------------------

            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                decimal totalAmount = 0;

                // Capture ONE timestamp for this submitted transaction.
                // All products added in this transaction will have
                // the same AddedAt time.

                var transactionTime = DateTime.Now;

                // -------------------------------------------------
                // CALCULATE TOTAL FIRST
                // -------------------------------------------------

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

                    decimal subtotal =
                        product.Price * quantity;

                    totalAmount += subtotal;
                }

                // -------------------------------------------------
                // CASH SALE
                // -------------------------------------------------

                if (TransactionType == "Cash")
                {
                    if (AmountPaid < totalAmount)
                    {
                        throw new Exception(
                            "Amount paid must cover the total amount."
                        );
                    }

                    // Cash transactions always create a NEW Sale.

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

                    await _context.SaveChangesAsync();

                    // ---------------------------------------------
                    // CREATE CASH SALE ITEMS
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

                        product.Quantity -= quantity;
                    }
                }

                // -------------------------------------------------
                // UTANG SALE
                // -------------------------------------------------

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
                    // FIND EXISTING ACTIVE UTANG FOR THIS CUSTOMER
                    // ON THE SAME DAY
                    // ---------------------------------------------

                    var today = transactionTime.Date;
                    var tomorrow = today.AddDays(1);

                    var existingUtang = await _context.Utangs
                        .Include(u => u.Sale)
                            .ThenInclude(s => s!.SaleItems)
                        .Where(u =>
                            u.CustomerId == CustomerId.Value &&
                            u.Balance > 0 &&
                            u.Sale != null &&
                            u.Sale.SaleDate >= today &&
                            u.Sale.SaleDate < tomorrow
                        )
                        .OrderByDescending(u => u.UtangId)
                        .FirstOrDefaultAsync();

                    // =============================================
                    // EXISTING UTANG FOUND
                    // =============================================

                    if (existingUtang != null &&
                        existingUtang.Sale != null)
                    {
                        var sale = existingUtang.Sale;

                        // -----------------------------------------
                        // ADD NEW PRODUCTS TO EXISTING SALE
                        // -----------------------------------------

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

                            product.Quantity -= quantity;
                        }

                        // -----------------------------------------
                        // UPDATE EXISTING SALE
                        // -----------------------------------------

                        sale.TotalAmount += totalAmount;

                        sale.AmountPaid += AmountPaid;

                        sale.PaymentMethod = "Utang";

                        decimal newBalance =
                            sale.TotalAmount - sale.AmountPaid;

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
                        // UPDATE EXISTING UTANG
                        // -----------------------------------------

                        existingUtang.Amount += totalAmount;

                        existingUtang.AmountPaid += AmountPaid;

                        existingUtang.Balance =
                            Math.Max(
                                0,
                                existingUtang.Amount -
                                existingUtang.AmountPaid
                            );

                        existingUtang.Status =
                            sale.Status;
                    }

                    // =============================================
                    // NO EXISTING UTANG
                    // CREATE NEW UTANG
                    // =============================================

                    else
                    {
                        decimal balance =
                            totalAmount - AmountPaid;

                        string status =
                            AmountPaid >= totalAmount
                                ? "Paid"
                                : AmountPaid > 0
                                    ? "Partial"
                                    : "Unpaid";

                        // -----------------------------------------
                        // CREATE NEW SALE
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

                        await _context.SaveChangesAsync();

                        // -----------------------------------------
                        // CREATE SALE ITEMS
                        // -----------------------------------------

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

                            product.Quantity -= quantity;
                        }

                        // -----------------------------------------
                        // CREATE UTANG RECORD
                        // -----------------------------------------

                        var utang = new Utang
                        {
                            CustomerId = CustomerId.Value,
                            SaleId = sale.SaleId,
                            Amount = totalAmount,
                            AmountPaid = AmountPaid,
                            Balance = balance,
                            DateCreated = transactionTime,
                            Status = status
                        };

                        _context.Utangs.Add(utang);
                    }
                }

                // -------------------------------------------------
                // SAVE EVERYTHING
                // -------------------------------------------------

                await _context.SaveChangesAsync();

                // -------------------------------------------------
                // COMMIT DATABASE TRANSACTION
                // -------------------------------------------------

                await transaction.CommitAsync();

                // -------------------------------------------------
                // RETURN TO SALES PAGE
                // -------------------------------------------------

                return returnToUtang
                    ? RedirectToAction("Index", "Utang")
                    : RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // -----------------------------------------------
                // ROLLBACK IF SOMETHING GOES WRONG
                // -----------------------------------------------

                await transaction.RollbackAsync();

                if (returnToUtang)
                {
                    TempData["TransactionError"] = ex.Message;
                    return RedirectToAction("Index", "Utang");
                }

                ModelState.AddModelError(
                    "",
                    ex.Message
                );

                await LoadTransactionData();

                var sales = await GetSales();

                return View("Index", sales);
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
        // LOAD CUSTOMERS AND PRODUCTS FOR MODAL
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
    }
}
