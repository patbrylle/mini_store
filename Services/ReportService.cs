using System.Globalization;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Models;

namespace mini_store.Services
{
    public interface IReportService
    {
        Task<OverviewVm> OverviewAsync();
        Task<ReportTable> BuildAsync(ReportFilter filter);
    }

    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _db;

        public ReportService(ApplicationDbContext db) { _db = db; }

        // =====================================================
        // ENTRY POINT
        // =====================================================

        public async Task<ReportTable> BuildAsync(ReportFilter f)
        {
            var info = ReportCatalog.Normalize(f);
            var range = info.HasRange ? ReportRanges.Resolve(f, info.DefaultRange) : ReportRanges.AllTime;

            var table = info.Key switch
            {
                "sales"       => await SalesAsync(f, range),
                "daily"       => await DailyAsync(f, range),
                "bestsellers" => await BestSellersAsync(f, range),
                "payments"    => await PaymentsAsync(f, range),
                "products"    => await ProductsAsync(f, lowOnly: false),
                "lowstock"    => await ProductsAsync(f, lowOnly: true),
                "customers"   => await CustomersAsync(f),
                "customertx"  => await CustomerTransactionsAsync(f, range),
                "utang"       => await UtangAsync(f, range),
                _ => throw new ArgumentException("Unknown report: " + info.Key)
            };

            table.Subtitle = await DescribeAsync(f, info, range);
            return table;
        }

        // =====================================================
        // SMALL HELPERS
        // =====================================================

        // same format as Sale.TransactionNo / Payment.ReceiptNo (single source of truth)
        private static string TxnNo(int saleId, DateTime date) =>
            new Sale { SaleId = saleId, SaleDate = date }.TransactionNo;

        private static string ReceiptNo(int saleId, int sequence, DateTime paidAt) =>
            new Payment { SaleId = saleId, Sequence = sequence, PaidAt = paidAt }.ReceiptNo;

        private static string SaleStatus(decimal total, decimal paid) =>
            paid >= total ? "Paid" : paid > 0 ? "Partial" : "Unpaid";

        private static string Money(decimal v) => ReportFormat.Money(v);
        private static string Int(int v) => ReportFormat.Int(v);

        private async Task<string> DescribeAsync(ReportFilter f, ReportInfo info, DateRange r)
        {
            var parts = new List<string>();

            if (info.HasRange) parts.Add("Period: " + r.Label);
            if (info.HasStatus && f.Status != null) parts.Add("Status: " + f.Status);

            if (info.HasCategory && f.CategoryId is int cat)
            {
                var name = await _db.Categories.Where(c => c.CategoryId == cat)
                    .Select(c => c.CategoryName).FirstOrDefaultAsync();
                if (name != null) parts.Add("Category: " + name);
            }

            if (info.HasCustomer && f.CustomerId is int cid)
            {
                var name = await _db.Customers.Where(c => c.CustomerId == cid)
                    .Select(c => c.CustomerName).FirstOrDefaultAsync();
                if (name != null) parts.Add("Customer: " + name);
            }

            return parts.Count == 0 ? "All records" : string.Join("   |   ", parts);
        }

        // =====================================================
        // SALES
        // =====================================================

        private IQueryable<Sale> SalesFilter(ReportFilter f, DateRange r, bool customersOnly)
        {
            var q = _db.Sales.AsNoTracking().AsQueryable();

            if (r.Start is DateTime start && r.EndExclusive is DateTime end)
                q = q.Where(s => s.SaleDate >= start && s.SaleDate < end);

            if (f.CustomerId is int cid) q = q.Where(s => s.CustomerId == cid);
            else if (customersOnly) q = q.Where(s => s.CustomerId != null);

            return q;
        }

        private sealed class SaleRow
        {
            public int SaleId { get; set; }
            public DateTime SaleDate { get; set; }
            public string? Customer { get; set; }
            public string PaymentMethod { get; set; } = "";
            public int Items { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal AmountPaid { get; set; }
            public string? CreatedBy { get; set; }
        }

        private static IQueryable<SaleRow> ProjectSales(IQueryable<Sale> q) =>
            q.Select(s => new SaleRow
            {
                SaleId = s.SaleId,
                SaleDate = s.SaleDate,
                Customer = s.Customer != null ? s.Customer.CustomerName : null,
                PaymentMethod = s.PaymentMethod,
                Items = s.SaleItems.Sum(i => i.Quantity),
                TotalAmount = s.TotalAmount,
                AmountPaid = s.AmountPaid,
                CreatedBy = s.CreatedBy
            });

        private async Task<ReportTable> SalesAsync(ReportFilter f, DateRange r)
        {
            var rows = await ProjectSales(
                SalesFilter(f, r, customersOnly: false).OrderByDescending(s => s.SaleDate)).ToListAsync();

            if (f.Status != null)
                rows = rows.Where(x => SaleStatus(x.TotalAmount, x.AmountPaid) == f.Status).ToList();

            var t = new ReportTable { Key = "sales", Title = "Sales Report" }
                .Col("Transaction No", CellKind.Text, 1.5f)
                .Col("Date & Time", CellKind.DateTime, 1.5f)
                .Col("Customer", CellKind.Text, 1.5f)
                .Col("Type", CellKind.Text, 0.8f)
                .Col("Items", CellKind.Integer, 0.7f)
                .Col("Total", CellKind.Money, 1.1f)
                .Col("Paid", CellKind.Money, 1.1f)
                .Col("Balance", CellKind.Money, 1.1f)
                .Col("Status", CellKind.Status, 0.9f)
                .Col("Cashier", CellKind.Text, 1.6f);

            foreach (var x in rows)
                t.Rows.Add(new object?[]
                {
                    TxnNo(x.SaleId, x.SaleDate), x.SaleDate, x.Customer ?? "Walk-in", x.PaymentMethod,
                    x.Items, x.TotalAmount, x.AmountPaid, x.TotalAmount - x.AmountPaid,
                    SaleStatus(x.TotalAmount, x.AmountPaid), x.CreatedBy ?? ""
                });

            var total = rows.Sum(x => x.TotalAmount);
            var paid = rows.Sum(x => x.AmountPaid);

            t.Stat("Transactions", Int(rows.Count))
             .Stat("Total sales", Money(total), "gold")
             .Stat("Paid", Money(paid), "success")
             .Stat("Outstanding", Money(total - paid), "danger")
             .Stat("Average sale", Money(rows.Count > 0 ? total / rows.Count : 0m));

            if (rows.Count > 0)
                t.SetTotals((4, rows.Sum(x => x.Items)), (5, total), (6, paid), (7, total - paid));

            return t;
        }

        private async Task<ReportTable> CustomerTransactionsAsync(ReportFilter f, DateRange r)
        {
            var rows = await ProjectSales(
                SalesFilter(f, r, customersOnly: true)
                    .OrderBy(s => s.Customer!.CustomerName)
                    .ThenByDescending(s => s.SaleDate)).ToListAsync();

            if (f.Status != null)
                rows = rows.Where(x => SaleStatus(x.TotalAmount, x.AmountPaid) == f.Status).ToList();

            var t = new ReportTable { Key = "customertx", Title = "Customer Transactions Report" }
                .Col("Customer", CellKind.Text, 1.7f)
                .Col("Date & Time", CellKind.DateTime, 1.5f)
                .Col("Transaction No", CellKind.Text, 1.5f)
                .Col("Type", CellKind.Text, 0.8f)
                .Col("Total", CellKind.Money, 1.1f)
                .Col("Paid", CellKind.Money, 1.1f)
                .Col("Balance", CellKind.Money, 1.1f)
                .Col("Status", CellKind.Status, 0.9f);

            foreach (var x in rows)
                t.Rows.Add(new object?[]
                {
                    x.Customer ?? "", x.SaleDate, TxnNo(x.SaleId, x.SaleDate), x.PaymentMethod,
                    x.TotalAmount, x.AmountPaid, x.TotalAmount - x.AmountPaid,
                    SaleStatus(x.TotalAmount, x.AmountPaid)
                });

            var total = rows.Sum(x => x.TotalAmount);
            var paid = rows.Sum(x => x.AmountPaid);

            t.Stat("Customers", Int(rows.Select(x => x.Customer).Distinct().Count()))
             .Stat("Transactions", Int(rows.Count))
             .Stat("Total", Money(total), "gold")
             .Stat("Paid", Money(paid), "success")
             .Stat("Outstanding", Money(total - paid), "danger");

            if (rows.Count > 0) t.SetTotals((4, total), (5, paid), (6, total - paid));
            return t;
        }

        private async Task<ReportTable> DailyAsync(ReportFilter f, DateRange r)
        {
            var days = await SalesFilter(f, r, customersOnly: false)
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new
                {
                    Day = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(x => x.TotalAmount),
                    Paid = g.Sum(x => x.AmountPaid)
                })
                .OrderByDescending(x => x.Day)
                .ToListAsync();

            var t = new ReportTable { Key = "daily", Title = "Daily Sales Summary" }
                .Col("Date", CellKind.Date, 1.2f)
                .Col("Day", CellKind.Text, 1.1f)
                .Col("Transactions", CellKind.Integer, 1f)
                .Col("Total sales", CellKind.Money, 1.2f)
                .Col("Paid", CellKind.Money, 1.2f)
                .Col("Balance", CellKind.Money, 1.2f);

            foreach (var d in days)
                t.Rows.Add(new object?[]
                {
                    d.Day, d.Day.ToString("dddd", CultureInfo.InvariantCulture),
                    d.Count, d.Total, d.Paid, d.Total - d.Paid
                });

            var total = days.Sum(d => d.Total);
            var paid = days.Sum(d => d.Paid);
            var count = days.Sum(d => d.Count);

            t.Stat("Days with sales", Int(days.Count))
             .Stat("Transactions", Int(count))
             .Stat("Total sales", Money(total), "gold")
             .Stat("Paid", Money(paid), "success")
             .Stat("Outstanding", Money(total - paid), "danger")
             .Stat("Average per day", Money(days.Count > 0 ? total / days.Count : 0m));

            if (days.Count > 0) t.SetTotals((2, count), (3, total), (4, paid), (5, total - paid));
            return t;
        }

        // =====================================================
        // BEST SELLERS
        // =====================================================

        private async Task<ReportTable> BestSellersAsync(ReportFilter f, DateRange r)
        {
            var q = _db.SaleItems.AsNoTracking().AsQueryable();

            if (r.Start is DateTime start && r.EndExclusive is DateTime end)
                q = q.Where(i => i.Sale!.SaleDate >= start && i.Sale.SaleDate < end);

            if (f.CategoryId is int cat) q = q.Where(i => i.Product!.CategoryId == cat);

            var rows = await q
                .GroupBy(i => new { i.ProductId, i.Product!.ProductName, Category = i.Product.Category!.CategoryName })
                .Select(g => new
                {
                    g.Key.ProductName,
                    g.Key.Category,
                    Qty = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(x => x.Qty)
                .ThenByDescending(x => x.Revenue)
                .ToListAsync();

            var t = new ReportTable { Key = "bestsellers", Title = "Best Sellers Report" }
                .Col("Rank", CellKind.Integer, 0.6f)
                .Col("Product", CellKind.Text, 2.4f)
                .Col("Category", CellKind.Text, 1.4f)
                .Col("Units sold", CellKind.Integer, 1f)
                .Col("Revenue", CellKind.Money, 1.2f);

            var rank = 1;
            foreach (var x in rows)
                t.Rows.Add(new object?[] { rank++, x.ProductName ?? "(deleted product)", x.Category ?? "", x.Qty, x.Revenue });

            t.Stat("Products sold", Int(rows.Count))
             .Stat("Units sold", Int(rows.Sum(x => x.Qty)), "gold")
             .Stat("Revenue", Money(rows.Sum(x => x.Revenue)), "success");

            if (rows.Count > 0) t.SetTotals((3, rows.Sum(x => x.Qty)), (4, rows.Sum(x => x.Revenue)));
            return t;
        }

        // =====================================================
        // PAYMENTS
        // =====================================================

        private sealed class PaymentRow
        {
            public int SaleId { get; set; }
            public DateTime SaleDate { get; set; }
            public int Sequence { get; set; }
            public DateTime PaidAt { get; set; }
            public string? Customer { get; set; }
            public string Method { get; set; } = "";
            public decimal Amount { get; set; }
            public string? ReceivedBy { get; set; }
        }

        private async Task<ReportTable> PaymentsAsync(ReportFilter f, DateRange r)
        {
            var q = _db.Payments.AsNoTracking().AsQueryable();

            if (r.Start is DateTime start && r.EndExclusive is DateTime end)
                q = q.Where(p => p.PaidAt >= start && p.PaidAt < end);

            if (f.CustomerId is int cid) q = q.Where(p => p.Sale!.CustomerId == cid);

            var rows = await q.OrderByDescending(p => p.PaidAt)
                .Select(p => new PaymentRow
                {
                    SaleId = p.SaleId,
                    SaleDate = p.Sale!.SaleDate,
                    Sequence = p.Sequence,
                    PaidAt = p.PaidAt,
                    Customer = p.Sale.Customer != null ? p.Sale.Customer.CustomerName : null,
                    Method = p.Method,
                    Amount = p.Amount,
                    ReceivedBy = p.ReceivedBy
                })
                .ToListAsync();

            var t = new ReportTable { Key = "payments", Title = "Payments & Collections Report" }
                .Col("Receipt No", CellKind.Text, 1.7f)
                .Col("Date & Time", CellKind.DateTime, 1.5f)
                .Col("Customer", CellKind.Text, 1.5f)
                .Col("Transaction No", CellKind.Text, 1.6f)
                .Col("Type", CellKind.Text, 1.1f)
                .Col("Method", CellKind.Text, 0.8f)
                .Col("Amount", CellKind.Money, 1.1f)
                .Col("Received by", CellKind.Text, 1.6f);

            foreach (var x in rows)
                t.Rows.Add(new object?[]
                {
                    ReceiptNo(x.SaleId, x.Sequence, x.PaidAt), x.PaidAt, x.Customer ?? "Walk-in",
                    TxnNo(x.SaleId, x.SaleDate), x.Sequence == 1 ? "At sale" : "Utang payment",
                    x.Method, x.Amount, x.ReceivedBy ?? ""
                });

            var all = rows.Sum(x => x.Amount);
            var atSale = rows.Where(x => x.Sequence == 1).Sum(x => x.Amount);

            t.Stat("Receipts", Int(rows.Count))
             .Stat("Total collected", Money(all), "success")
             .Stat("Paid at sale", Money(atSale), "gold")
             .Stat("Utang collections", Money(all - atSale));

            if (rows.Count > 0) t.SetTotals((6, all));
            return t;
        }

        // =====================================================
        // PRODUCTS / LOW STOCK
        // =====================================================

        private async Task<ReportTable> ProductsAsync(ReportFilter f, bool lowOnly)
        {
            var q = _db.Products.AsNoTracking().AsQueryable();

            if (lowOnly) q = q.Where(p => p.Quantity <= StockRules.LowStock);
            if (f.CategoryId is int cat) q = q.Where(p => p.CategoryId == cat);

            var ordered = lowOnly
                ? q.OrderBy(p => p.Quantity).ThenBy(p => p.ProductName)
                : q.OrderBy(p => p.Category!.CategoryName).ThenBy(p => p.ProductName);

            var items = await ordered.Select(p => new
            {
                Name = p.ProductName,
                Category = p.Category != null ? p.Category.CategoryName : "",
                p.Price,
                p.Quantity
            }).ToListAsync();

            if (f.Status != null)
                items = items.Where(i => StockRules.Status(i.Quantity) == f.Status).ToList();

            var t = new ReportTable
            {
                Key = lowOnly ? "lowstock" : "products",
                Title = lowOnly ? "Low & Out of Stock Report" : "Product Inventory Report"
            }
                .Col("Product", CellKind.Text, 2.4f)
                .Col("Category", CellKind.Text, 1.4f)
                .Col("Price", CellKind.Money, 1f)
                .Col("Quantity", CellKind.Integer, 0.9f)
                .Col("Stock value", CellKind.Money, 1.2f)
                .Col("Status", CellKind.Status, 1f);

            foreach (var i in items)
                t.Rows.Add(new object?[]
                {
                    i.Name ?? "", i.Category, i.Price, i.Quantity, i.Price * i.Quantity,
                    StockRules.Status(i.Quantity)
                });

            var low = items.Count(i => StockRules.Status(i.Quantity) == "Low Stock");
            var outOf = items.Count(i => StockRules.Status(i.Quantity) == "Out of Stock");

            if (lowOnly)
            {
                t.Stat("Need restocking", Int(items.Count), "gold")
                 .Stat("Low stock", Int(low), "gold")
                 .Stat("Out of stock", Int(outOf), "danger");
            }
            else
            {
                t.Stat("Products", Int(items.Count))
                 .Stat("Units in stock", Int(items.Sum(i => i.Quantity)), "gold")
                 .Stat("Stock value", Money(items.Sum(i => i.Price * i.Quantity)), "success")
                 .Stat("Low stock", Int(low), "gold")
                 .Stat("Out of stock", Int(outOf), "danger");
            }

            if (items.Count > 0)
                t.SetTotals((3, items.Sum(i => i.Quantity)), (4, items.Sum(i => i.Price * i.Quantity)));

            return t;
        }

        // =====================================================
        // CUSTOMERS
        // =====================================================

        private async Task<ReportTable> CustomersAsync(ReportFilter f)
        {
            var customers = await _db.Customers.AsNoTracking().OrderBy(c => c.CustomerName).ToListAsync();

            var purchases = (await _db.Sales.AsNoTracking()
                    .Where(s => s.CustomerId != null)
                    .GroupBy(s => s.CustomerId)
                    .Select(g => new { Id = g.Key, Count = g.Count(), Total = g.Sum(x => x.TotalAmount) })
                    .ToListAsync())
                .Where(x => x.Id != null)
                .ToDictionary(x => x.Id!.Value, x => (x.Count, x.Total));

            var owed = (await _db.Utangs.AsNoTracking()
                    .Where(u => u.Balance > 0)
                    .GroupBy(u => u.CustomerId)
                    .Select(g => new { Id = g.Key, Balance = g.Sum(x => x.Balance) })
                    .ToListAsync())
                .ToDictionary(x => x.Id, x => x.Balance);

            var rows = new List<(Customer C, bool Login, int Count, decimal Total, decimal Owed)>();
            foreach (var c in customers)
            {
                purchases.TryGetValue(c.CustomerId, out var p);
                owed.TryGetValue(c.CustomerId, out var o);
                rows.Add((c, !string.IsNullOrEmpty(c.UserId), p.Count, p.Total, o));
            }

            rows = f.Status switch
            {
                "With Login" => rows.Where(x => x.Login).ToList(),
                "No Login"   => rows.Where(x => !x.Login).ToList(),
                "Owes Utang" => rows.Where(x => x.Owed > 0).ToList(),
                _ => rows
            };

            var t = new ReportTable { Key = "customers", Title = "Customer Report" }
                .Col("Customer", CellKind.Text, 1.6f)
                .Col("Contact", CellKind.Text, 1.1f)
                .Col("Email", CellKind.Text, 1.8f)
                .Col("Address", CellKind.Text, 2f)
                .Col("Login", CellKind.Text, 0.6f)
                .Col("Transactions", CellKind.Integer, 1f)
                .Col("Total purchases", CellKind.Money, 1.2f)
                .Col("Balance owed", CellKind.Money, 1.2f);

            foreach (var x in rows)
                t.Rows.Add(new object?[]
                {
                    x.C.CustomerName, x.C.ContactNumber ?? "", x.C.Email ?? "", x.C.Address ?? "",
                    x.Login ? "Yes" : "No", x.Count, x.Total, x.Owed
                });

            t.Stat("Customers", Int(rows.Count))
             .Stat("With login", Int(rows.Count(x => x.Login)), "success")
             .Stat("Owe utang", Int(rows.Count(x => x.Owed > 0)), "gold")
             .Stat("Total owed", Money(rows.Sum(x => x.Owed)), "danger");

            if (rows.Count > 0)
                t.SetTotals((5, rows.Sum(x => x.Count)), (6, rows.Sum(x => x.Total)), (7, rows.Sum(x => x.Owed)));

            return t;
        }

        // =====================================================
        // UTANG / CREDIT RECORDS
        // =====================================================

        private sealed class UtangRow
        {
            public int UtangId { get; set; }
            public string? Customer { get; set; }
            public int SaleId { get; set; }
            public DateTime SaleDate { get; set; }
            public DateTime Created { get; set; }
            public DateTime? Due { get; set; }
            public decimal Amount { get; set; }
            public decimal Paid { get; set; }
            public decimal Balance { get; set; }
        }

        private static string UtangStatus(UtangRow u) =>
            u.Balance <= 0 ? "Paid"
            : u.Due is DateTime d && d.Date < DateTime.Today ? "Overdue"
            : u.Paid > 0 ? "Partial"
            : "Unpaid";

        private async Task<ReportTable> UtangAsync(ReportFilter f, DateRange r)
        {
            var q = _db.Utangs.AsNoTracking().AsQueryable();

            if (r.Start is DateTime start && r.EndExclusive is DateTime end)
                q = q.Where(u => u.DateCreated >= start && u.DateCreated < end);

            if (f.CustomerId is int cid) q = q.Where(u => u.CustomerId == cid);

            var rows = await q.OrderByDescending(u => u.DateCreated)
                .Select(u => new UtangRow
                {
                    UtangId = u.UtangId,
                    Customer = u.Customer != null ? u.Customer.CustomerName : null,
                    SaleId = u.SaleId,
                    SaleDate = u.Sale != null ? u.Sale.SaleDate : u.DateCreated,
                    Created = u.DateCreated,
                    Due = u.DueDate,
                    Amount = u.Amount,
                    Paid = u.AmountPaid,
                    Balance = u.Balance
                })
                .ToListAsync();

            if (f.Status != null) rows = rows.Where(x => UtangStatus(x) == f.Status).ToList();

            var t = new ReportTable { Key = "utang", Title = "Credit / Utang Records Report" }
                .Col("Utang #", CellKind.Integer, 0.7f)
                .Col("Customer", CellKind.Text, 1.7f)
                .Col("Transaction No", CellKind.Text, 1.6f)
                .Col("Date created", CellKind.Date, 1.2f)
                .Col("Due date", CellKind.Date, 1.2f)
                .Col("Amount", CellKind.Money, 1.1f)
                .Col("Paid", CellKind.Money, 1.1f)
                .Col("Balance", CellKind.Money, 1.1f)
                .Col("Status", CellKind.Status, 1f);

            foreach (var x in rows)
                t.Rows.Add(new object?[]
                {
                    x.UtangId, x.Customer ?? "", TxnNo(x.SaleId, x.SaleDate), x.Created, x.Due,
                    x.Amount, x.Paid, x.Balance, UtangStatus(x)
                });

            var overdue = rows.Where(x => UtangStatus(x) == "Overdue").ToList();

            t.Stat("Records", Int(rows.Count))
             .Stat("Total utang", Money(rows.Sum(x => x.Amount)), "gold")
             .Stat("Collected", Money(rows.Sum(x => x.Paid)), "success")
             .Stat("Remaining balance", Money(rows.Sum(x => x.Balance)), "danger")
             .Stat("Overdue", Int(overdue.Count), "danger", Money(overdue.Sum(x => x.Balance)) + " unpaid");

            if (rows.Count > 0)
                t.SetTotals((5, rows.Sum(x => x.Amount)), (6, rows.Sum(x => x.Paid)), (7, rows.Sum(x => x.Balance)));

            return t;
        }

        // =====================================================
        // OVERVIEW
        // =====================================================

        public async Task<OverviewVm> OverviewAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            var monthStart = new DateTime(today.Year, today.Month, 1);

            async Task<(decimal Total, int Count)> Period(DateTime? start)
            {
                var q = _db.Sales.AsNoTracking().AsQueryable();
                if (start is DateTime s) q = q.Where(x => x.SaleDate >= s && x.SaleDate < tomorrow);
                var total = await q.SumAsync(x => x.TotalAmount);
                var count = await q.CountAsync();
                return (total, count);
            }

            var (todayTotal, todayCount) = await Period(today);
            var (weekTotal, weekCount) = await Period(weekStart);
            var (monthTotal, monthCount) = await Period(monthStart);
            var (allTotal, allCount) = await Period(null);

            var paid = await _db.Sales.SumAsync(s => s.AmountPaid);
            var outstanding = allTotal - paid;
            // rounded DOWN, so 99.6% never shows as 100%
            var rate = allTotal > 0 ? Math.Floor(paid / allTotal * 1000m) / 10m : 0m;

            var overdueQ = _db.Utangs.Where(u => u.Balance > 0 && u.DueDate != null && u.DueDate < today);
            var overdueCount = await overdueQ.CountAsync();
            var overdueAmount = await overdueQ.SumAsync(u => u.Balance);

            var products = await _db.Products.CountAsync();
            var units = await _db.Products.SumAsync(p => p.Quantity);
            var stockValue = await _db.Products.SumAsync(p => p.Price * p.Quantity);
            var low = await _db.Products.CountAsync(p => p.Quantity > 0 && p.Quantity <= StockRules.LowStock);
            var outOfStock = await _db.Products.CountAsync(p => p.Quantity <= 0);

            var vm = new OverviewVm
            {
                Paid = paid,
                Outstanding = outstanding,
                LowStock = low,
                OutOfStock = outOfStock,
                Healthy = Math.Max(products - low - outOfStock, 0)
            };

            vm.Sections.Add(new OverviewSection("Sales", "bi-cart-check")
            {
                Stats =
                {
                    new ReportStat("Today", Money(todayTotal), "primary", Int(todayCount) + " transactions"),
                    new ReportStat("This week", Money(weekTotal), "gold", Int(weekCount) + " transactions"),
                    new ReportStat("This month", Money(monthTotal), "success", Int(monthCount) + " transactions"),
                    new ReportStat("All time", Money(allTotal), "primary", Int(allCount) + " transactions")
                }
            });

            vm.Sections.Add(new OverviewSection("Collections", "bi-piggy-bank")
            {
                Stats =
                {
                    new ReportStat("Collected", Money(paid), "success"),
                    new ReportStat("Outstanding", Money(outstanding), "danger"),
                    new ReportStat("Collection rate", rate.ToString("0.#", CultureInfo.InvariantCulture) + "%", "primary", "of all sales"),
                    new ReportStat("Overdue utang", Int(overdueCount), "danger", Money(overdueAmount) + " unpaid")
                }
            });

            vm.Sections.Add(new OverviewSection("Inventory", "bi-box-seam")
            {
                Stats =
                {
                    new ReportStat("Products", Int(products), "primary"),
                    new ReportStat("Units in stock", Int(units), "gold", "Worth " + Money(stockValue)),
                    new ReportStat("Low stock", Int(low), "gold", $"{StockRules.LowStock} or fewer left"),
                    new ReportStat("Out of stock", Int(outOfStock), "danger")
                }
            });

            // last 7 days (missing days count as zero)
            var start7 = today.AddDays(-6);
            var daily = await _db.Sales.AsNoTracking()
                .Where(s => s.SaleDate >= start7 && s.SaleDate < tomorrow)
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new { Day = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .ToListAsync();

            var map = daily.ToDictionary(x => x.Day.Date, x => x.Total);
            for (var i = 0; i < 7; i++)
            {
                var day = start7.AddDays(i);
                vm.Last7Labels.Add(day.ToString("ddd d", CultureInfo.InvariantCulture));
                vm.Last7Values.Add(map.TryGetValue(day, out var v) ? v : 0m);
            }

            // top 5 best sellers
            var best = await _db.SaleItems.AsNoTracking()
                .GroupBy(i => new { i.ProductId, i.Product!.ProductName })
                .Select(g => new BestRow
                {
                    Name = g.Key.ProductName ?? "",
                    Qty = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(x => x.Qty)
                .Take(5)
                .ToListAsync();

            vm.Best.AddRange(best);
            return vm;
        }
    }
}