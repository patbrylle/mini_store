using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Models;

namespace mini_store.Services
{
    public interface ISalesService
    {
        Task<TransactionFormData> GetFormDataAsync();
        Task<TransactionVm?> GetTransactionAsync(int saleId);
        Task<SaleResult> CreateSaleAsync(CreateSaleRequest request, string? cashier);
        Task<SaleResult> PayUtangAsync(int utangId, decimal amount, string? method, string? referenceNo, string? cashier);
Task<SaleResult> DeleteSaleAsync(int saleId, bool restock);
        Task<int> CreateOverdueNotificationsAsync();
        Task<CustomerHistoryVm?> GetCustomerHistoryAsync(int customerId);
Task<Dictionary<int, decimal>> GetUtangBalancesAsync();
    }

    public class SalesService : ISalesService
    {
private const int LowStock = StockRules.LowStock;
          private readonly ApplicationDbContext _db;
    private readonly ILogger<SalesService> _log;
    private readonly INotificationService _notify;

    public SalesService(
        ApplicationDbContext db,
        ILogger<SalesService> log,
        INotificationService notify)
    {
        _db = db;
        _log = log;
        _notify = notify;
    }
        public async Task<CustomerHistoryVm?> GetCustomerHistoryAsync(int customerId)
{
    var customer = await _db.Customers.AsNoTracking()
        .FirstOrDefaultAsync(c => c.CustomerId == customerId);

    if (customer == null) return null;

    return new CustomerHistoryVm
    {
        Customer = customer,
        Sales = await _db.Sales.AsNoTracking()
            .Include(s => s.SaleItems)
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(),
        Utangs = await _db.Utangs.AsNoTracking()
            .Include(u => u.Sale)
            .Where(u => u.CustomerId == customerId)
            .OrderByDescending(u => u.DateCreated)
            .ToListAsync()
    };
}

// customerId -> total unpaid balance (only customers who owe)
public Task<Dictionary<int, decimal>> GetUtangBalancesAsync() =>
    _db.Utangs.AsNoTracking()
        .Where(u => u.Balance > 0)
        .GroupBy(u => u.CustomerId)
        .Select(g => new { CustomerId = g.Key, Balance = g.Sum(x => x.Balance) })
        .ToDictionaryAsync(x => x.CustomerId, x => x.Balance);


        private sealed class BusinessException : Exception
        {
            public BusinessException(string message) : base(message) { }
        }

        private static SaleResult Fail(string message) => new() { Success = false, Error = message };
        private static SaleResult Ok(int saleId, int? paymentId) =>
            new() { Success = true, SaleId = saleId, PaymentId = paymentId };

        private static string NormalizeMethod(string? method) =>
            PaymentMethods.All.FirstOrDefault(m =>
                string.Equals(m, method, StringComparison.OrdinalIgnoreCase)) ?? "Cash";


// replaces the old Notify body; all existing Notify(...) calls keep working
private void Notify(string title, string message, string type, string link) =>
    _notify.Add(title, message, type, link);
    
        // =====================================================
        // READ
        // =====================================================

        public async Task<TransactionFormData> GetFormDataAsync() => new()
        {
            Customers = await _db.Customers.AsNoTracking()
                .OrderBy(c => c.CustomerName).ToListAsync(),

            Products = await _db.Products.AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.Quantity > 0)
                .OrderBy(p => p.ProductName).ToListAsync(),

            Categories = await _db.Categories.AsNoTracking()
                .OrderBy(c => c.CategoryName).ToListAsync()
        };

        public async Task<TransactionVm?> GetTransactionAsync(int saleId)
        {
            var sale = await _db.Sales.AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.SaleItems).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.SaleId == saleId);

            if (sale == null) return null;

            return new TransactionVm
            {
                Sale = sale,
                Utang = await _db.Utangs.AsNoTracking().FirstOrDefaultAsync(u => u.SaleId == saleId),
                Payments = await _db.Payments.AsNoTracking()
                    .Where(p => p.SaleId == saleId)
                    .OrderBy(p => p.Sequence).ToListAsync()
            };
        }

        // =====================================================
        // CREATE SALE (cash or utang)
        // =====================================================

        public async Task<SaleResult> CreateSaleAsync(CreateSaleRequest r, string? cashier)
        {
            var isUtang = string.Equals(r.TransactionType, "Utang", StringComparison.OrdinalIgnoreCase);
            var isCash = string.Equals(r.TransactionType, "Cash", StringComparison.OrdinalIgnoreCase);
            if (!isUtang && !isCash) return Fail("Invalid transaction type.");

            if (r.ProductId == null || r.Quantity == null ||
                r.ProductId.Length == 0 || r.ProductId.Length != r.Quantity.Length)
                return Fail("Please add at least one product.");

            if (r.Quantity.Any(q => q <= 0 || q > 100_000))
                return Fail("Invalid product quantity.");

            // Idempotency: the same form submitted twice returns the first result.
            var key = string.IsNullOrWhiteSpace(r.IdempotencyKey) ? null : r.IdempotencyKey.Trim();
            if (key is { Length: > 40 }) key = key[..40];

            if (key != null)
            {
                var existing = await FindByKeyAsync(key);
                if (existing != null) return existing;
            }

            var paid = Math.Round(r.AmountPaid, 2);
            if (paid < 0) return Fail("Amount paid cannot be negative.");

            var method = "Cash";

            if (isUtang)
            {
                if (r.CustomerId == null) return Fail("Please select a customer for utang.");
                if (r.DueDate == null) return Fail("Please choose a due date for the utang.");
                if (r.DueDate.Value.Date < DateTime.Today) return Fail("Due date cannot be in the past.");
            }

            Customer? customer = null;
            if (r.CustomerId.HasValue)
            {
                customer = await _db.Customers.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CustomerId == r.CustomerId.Value);
                if (customer == null) return Fail("The selected customer could not be found.");
            }

            // Merge duplicate lines so the stock check can't be bypassed.
            var lines = r.ProductId.Zip(r.Quantity, (p, q) => (ProductId: p, Qty: q))
                .GroupBy(x => x.ProductId)
                .Select(g => (ProductId: g.Key, Qty: g.Sum(x => x.Qty)))
                .ToList();

            var ids = lines.Select(l => l.ProductId).ToList();
            var products = await _db.Products.AsNoTracking()
                .Where(p => ids.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            decimal total = 0;
            foreach (var l in lines)
            {
                if (!products.TryGetValue(l.ProductId, out var p))
                    return Fail("One of the selected products no longer exists.");

                if (l.Qty > p.Quantity)
                    return Fail($"Not enough stock for {p.ProductName}. Only {p.Quantity} left.");

                total += p.Price * l.Qty;
            }

            if (isCash && paid < total) return Fail("Amount paid must cover the total amount.");
            if (isUtang && paid >= total)
                return Fail("The amount paid covers the total. Record it as a Cash Sale instead.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.Now;
                var applied = isCash ? total : paid;

                var sale = new Sale
                {
                    CustomerId = r.CustomerId,
                    SaleDate = now,
                    PaymentMethod = isUtang ? "Utang" : "Cash",
                    Status = TxStatus.From(total, applied),
                    TotalAmount = total,
                    AmountPaid = applied,
                    IdempotencyKey = key,
                    CreatedBy = cashier
                };

                // Atomic stock deduction: fails if someone else took the stock first.
                foreach (var l in lines)
                {
                    var id = l.ProductId;
                    var qty = l.Qty;

                    var rows = await _db.Products
                        .Where(p => p.ProductId == id && p.Quantity >= qty)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Quantity, p => p.Quantity - qty));

                    if (rows == 0)
                        throw new BusinessException(
                            $"Not enough stock for {products[id].ProductName}. Please refresh and try again.");

                    var price = products[id].Price;
                    sale.SaleItems.Add(new SaleItem
                    {
                        ProductId = id,
                        Quantity = qty,
                        UnitPrice = price,
                        Subtotal = price * qty,
                        AddedAt = now
                    });
                }

                _db.Sales.Add(sale);

                Utang? utang = null;
                if (isUtang)
                {
                    utang = new Utang
                    {
                        CustomerId = r.CustomerId!.Value,
                        Sale = sale,
                        Amount = total,
                        AmountPaid = applied,
                        Balance = total - applied,
                        DateCreated = now,
                        DueDate = r.DueDate!.Value.Date,
                        Status = sale.Status
                    };
                    _db.Utangs.Add(utang);
                }

                Payment? payment = null;
                if (applied > 0)
                {
                    payment = new Payment
                    {
                        Sale = sale,
                        Utang = utang,
                        Sequence = 1,
                        Amount = applied,
                        AmountTendered = isCash ? paid : applied,
                        ChangeGiven = isCash ? Math.Max(0, paid - total) : 0,
                        Method = method,
                        ReferenceNo = method == "Cash" ? null : r.ReferenceNo?.Trim(),
                        ReceivedBy = cashier,
                        PaidAt = now
                    };
                    _db.Payments.Add(payment);
                }

                // Stock notifications (compare before and after)
                var fresh = await _db.Products.AsNoTracking()
                    .Where(p => ids.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId, p => p.Quantity);

                foreach (var l in lines)
                    StockNotice(products[l.ProductId], products[l.ProductId].Quantity, fresh[l.ProductId]);

                var who = customer?.CustomerName ?? "Customer";
                Notify(isUtang ? "New Utang Sale" : "New Sale",
                       isUtang ? $"{who} recorded an utang of ₱{total:N2}."
                               : $"Cash sale recorded for ₱{total:N2}.",
                       "success", "/Sales");

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(sale.SaleId, payment?.PaymentId);
            }
            catch (BusinessException ex)
            {
                await tx.RollbackAsync();
                return Fail(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync();
                _log.LogError(ex, "Failed to save sale");

                // A double-click race can hit the unique key: return the first sale.
                if (key != null)
                {
                    _db.ChangeTracker.Clear();
                    var existing = await FindByKeyAsync(key);
                    if (existing != null) return existing;
                }

                return Fail("Could not save the transaction. Please try again.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _log.LogError(ex, "Failed to save sale");
                return Fail("Could not save the transaction. Please try again.");
            }
        }

        private async Task<SaleResult?> FindByKeyAsync(string key)
        {
            var saleId = await _db.Sales.AsNoTracking()
                .Where(s => s.IdempotencyKey == key)
                .Select(s => s.SaleId).FirstOrDefaultAsync();

            if (saleId == 0) return null;

            var paymentId = await _db.Payments.AsNoTracking()
                .Where(p => p.SaleId == saleId)
                .OrderBy(p => p.Sequence)
                .Select(p => (int?)p.PaymentId).FirstOrDefaultAsync();

            return Ok(saleId, paymentId);
        }

        // =====================================================
        // PAY UTANG
        // =====================================================

        public async Task<SaleResult> PayUtangAsync(
            int utangId, decimal amount, string? method, string? referenceNo, string? cashier)
        {
            amount = Math.Round(amount, 2);
            if (amount <= 0) return Fail("Please enter a valid payment amount.");

            method = "Cash";

            var utang = await _db.Utangs
                .Include(u => u.Customer)
                .Include(u => u.Sale)
                .FirstOrDefaultAsync(u => u.UtangId == utangId);

            if (utang == null) return Fail("Utang record not found.");
            if (utang.Balance <= 0) return Fail("This utang is already fully paid.");
            if (amount > utang.Balance) return Fail("Payment cannot be greater than the remaining balance.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                utang.AmountPaid += amount;
                utang.Balance = Math.Max(0, utang.Amount - utang.AmountPaid);
                utang.Status = TxStatus.From(utang.Amount, utang.AmountPaid);

                if (utang.Sale != null)
                {
                    utang.Sale.AmountPaid = utang.AmountPaid;
                    utang.Sale.Status = utang.Status;
                }

                var sequence = await _db.Payments.CountAsync(p => p.SaleId == utang.SaleId) + 1;

                var payment = new Payment
                {
                    SaleId = utang.SaleId,
                    UtangId = utang.UtangId,
                    Sequence = sequence,
                    Amount = amount,
                    AmountTendered = amount,
                    ChangeGiven = 0,
                    Method = method,
                    ReferenceNo = method == "Cash" ? null : referenceNo?.Trim(),
                    ReceivedBy = cashier,
                    PaidAt = DateTime.Now
                };
                _db.Payments.Add(payment);

                Notify("Utang Payment Received",
                       $"{utang.Customer?.CustomerName ?? "Customer"} paid ₱{amount:N2}. " +
                       $"Remaining balance: ₱{utang.Balance:N2}.",
                       utang.Balance <= 0 ? "success" : "info", "/Utang");

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(utang.SaleId, payment.PaymentId);
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                return Fail("This utang was just updated by someone else. Reopen it and try again.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _log.LogError(ex, "Failed to record utang payment");
                return Fail("Could not record the payment. Please try again.");
            }
        }

        // =====================================================
        // DELETE (paid transactions only, restores stock)
        // =====================================================

        public async Task<SaleResult> DeleteSaleAsync(int saleId, bool restock)
{
    var sale = await _db.Sales.Include(s => s.SaleItems)
        .FirstOrDefaultAsync(s => s.SaleId == saleId);

    if (sale == null) return Fail("Transaction not found.");
    if (!string.Equals(sale.Status, "Paid", StringComparison.OrdinalIgnoreCase))
        return Fail("Only fully paid transactions can be deleted.");

    await using var tx = await _db.Database.BeginTransactionAsync();
    try
    {
        if (restock)
        {
            foreach (var item in sale.SaleItems)
            {
                var pid = item.ProductId;
                var qty = item.Quantity;
                await _db.Products.Where(p => p.ProductId == pid)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Quantity, p => p.Quantity + qty));
            }
        }

        _db.Payments.RemoveRange(await _db.Payments.Where(p => p.SaleId == saleId).ToListAsync());

        var utang = await _db.Utangs.FirstOrDefaultAsync(u => u.SaleId == saleId);
        if (utang != null) _db.Utangs.Remove(utang);

        _db.SaleItems.RemoveRange(sale.SaleItems);
        _db.Sales.Remove(sale);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(saleId, null);
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        _log.LogError(ex, "Failed to delete sale {SaleId}", saleId);
        return Fail("Could not delete the transaction. Please try again.");
    }
}

        // =====================================================
        // OVERDUE NOTIFICATIONS (called by the background worker)
        // =====================================================

        public async Task<int> CreateOverdueNotificationsAsync()
        {
            var today = DateTime.Today;

            var overdue = await _db.Utangs.AsNoTracking()
                .Include(u => u.Customer)
                .Where(u => u.Balance > 0 && u.DueDate != null && u.DueDate < today)
                .ToListAsync();

            if (overdue.Count == 0) return 0;

            var links = overdue.Select(u => $"/Utang?highlight={u.UtangId}").ToList();

            var existing = (await _db.Notifications
                .Where(n => n.Title == "Overdue Utang" && links.Contains(n.Link))
                .Select(n => n.Link).ToListAsync()).ToHashSet();

            var added = 0;
            foreach (var u in overdue)
            {
                var link = $"/Utang?highlight={u.UtangId}";
                if (existing.Contains(link)) continue;

                Notify("Overdue Utang",
                       $"{u.Customer?.CustomerName ?? "Customer"}'s utang #{u.UtangId} is overdue. " +
                       $"Remaining balance: ₱{u.Balance:N2}.",
                       "danger", link);
                added++;
            }

            if (added > 0) await _db.SaveChangesAsync();
            return added;
        }

      

        private void StockNotice(Product product, int oldQty, int newQty)
        {
            static int Level(int q) => q <= 0 ? 2 : q <= LowStock ? 1 : 0;

            if (Level(newQty) <= Level(oldQty)) return;

            var link = $"/Products?productId={product.ProductId}";

            if (Level(newQty) == 2)
                Notify("Out of Stock", $"{product.ProductName} is out of stock.", "danger", link);
            else
                Notify("Low Stock",
                       $"{product.ProductName} has only {newQty} items remaining.", "warning", link);
        }
    }

    // Runs the overdue check on startup and every 6 hours.
    public class OverdueNotificationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<OverdueNotificationWorker> _log;

        public OverdueNotificationWorker(IServiceScopeFactory scopes, ILogger<OverdueNotificationWorker> log)
        {
            _scopes = scopes;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ISalesService>();
                    await service.CreateOverdueNotificationsAsync();
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Overdue notification run failed");
                }

                try { await Task.Delay(TimeSpan.FromHours(6), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }
}