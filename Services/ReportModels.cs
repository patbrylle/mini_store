using System.Globalization;

namespace mini_store.Services
{
    // ---------- shared stock rule (one place only) ----------
    public static class StockRules
    {
        /// "Low stock" = 1 up to this number, "Out of stock" = 0.
        public const int LowStock = 10;

        public static string Status(int qty) =>
            qty <= 0 ? "Out of Stock" : qty <= LowStock ? "Low Stock" : "In Stock";
    }

    // ---------- the one shape every report is turned into ----------
    public enum CellKind { Text, Money, Integer, Date, DateTime, Status }

    public record ReportColumn(string Header, CellKind Kind = CellKind.Text, float Weight = 1f)
    {
        public bool Right => Kind is CellKind.Money or CellKind.Integer;
    }

    public record ReportStat(string Label, string Value, string Tone = "primary", string? Sub = null);

    public class ReportTable
    {
        public string Key { get; init; } = "";
        public string Title { get; init; } = "";
        public string Subtitle { get; set; } = "";
        public List<ReportStat> Stats { get; } = new();
        public List<ReportColumn> Columns { get; } = new();
        public List<object?[]> Rows { get; } = new();
        public object?[]? Totals { get; private set; }

        public ReportTable Col(string header, CellKind kind = CellKind.Text, float weight = 1f)
        {
            Columns.Add(new ReportColumn(header, kind, weight));
            return this;
        }

        public ReportTable Stat(string label, string value, string tone = "primary", string? sub = null)
        {
            Stats.Add(new ReportStat(label, value, tone, sub));
            return this;
        }

        public void SetTotals(params (int Index, object? Value)[] cells)
        {
            var arr = new object?[Columns.Count];
            arr[0] = "TOTAL";
            foreach (var (index, value) in cells) arr[index] = value;
            Totals = arr;
        }
    }

    // ---------- overview page ----------
    public class BestRow
    {
        public string Name { get; set; } = "";
        public int Qty { get; set; }
        public decimal Revenue { get; set; }
    }

    public class OverviewSection
    {
        public OverviewSection(string title, string icon) { Title = title; Icon = icon; }
        public string Title { get; }
        public string Icon { get; }
        public List<ReportStat> Stats { get; } = new();
    }

    public class OverviewVm
    {
        public List<OverviewSection> Sections { get; } = new();
        public List<string> Last7Labels { get; } = new();
        public List<decimal> Last7Values { get; } = new();
        public decimal Paid { get; set; }
        public decimal Outstanding { get; set; }
        public int Healthy { get; set; }
        public int LowStock { get; set; }
        public int OutOfStock { get; set; }
        public List<BestRow> Best { get; } = new();
    }

    // ---------- filters coming from the page ----------
    public class ReportFilter
    {
        public string? Type { get; set; }
        public string? Range { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Status { get; set; }
        public int? CategoryId { get; set; }
        public int? CustomerId { get; set; }
    }

    public record ReportInfo(
        string Key, string Title, string Icon, string Group, string Blurb,
        bool HasRange = false, string DefaultRange = "month",
        string[]? Statuses = null, bool HasCategory = false, bool HasCustomer = false)
    {
        public bool HasStatus => Statuses is { Length: > 0 };
    }

    public static class ReportCatalog
    {
        public static readonly IReadOnlyList<ReportInfo> All = new[]
        {
            new ReportInfo("overview", "Overview", "bi-speedometer2", "Summary",
                "Key figures and charts at a glance."),

            new ReportInfo("sales", "Sales", "bi-receipt", "Sales",
                "Every sales transaction in the selected period.",
                HasRange: true, DefaultRange: "today",
                Statuses: new[] { "Paid", "Partial", "Unpaid" }, HasCustomer: true),

            new ReportInfo("daily", "Daily Sales Summary", "bi-calendar-week", "Sales",
                "Totals for each day. Use This Week or This Month for weekly and monthly sales.",
                HasRange: true, DefaultRange: "month"),

            new ReportInfo("bestsellers", "Best Sellers", "bi-trophy", "Sales",
                "Products ranked by units sold.",
                HasRange: true, DefaultRange: "month", HasCategory: true),

            new ReportInfo("payments", "Payments & Collections", "bi-cash-coin", "Sales",
                "Every payment received, including utang payments.",
                HasRange: true, DefaultRange: "today", HasCustomer: true),

            new ReportInfo("products", "Products", "bi-box-seam", "Inventory",
                "Full product list with stock levels and stock value.",
                Statuses: new[] { "In Stock", "Low Stock", "Out of Stock" }, HasCategory: true),

            new ReportInfo("lowstock", "Low & Out of Stock", "bi-exclamation-triangle", "Inventory",
                "Products that need restocking.",
                Statuses: new[] { "Low Stock", "Out of Stock" }, HasCategory: true),

            new ReportInfo("customers", "Customers", "bi-people", "Customers",
                "Customer directory with purchases and balance owed.",
                Statuses: new[] { "With Login", "No Login", "Owes Utang" }),

            new ReportInfo("customertx", "Customer Transactions", "bi-person-lines-fill", "Customers",
                "Transactions of registered customers, grouped by customer.",
                HasRange: true, DefaultRange: "month", HasCustomer: true,
                Statuses: new[] { "Paid", "Partial", "Unpaid" }),

            new ReportInfo("utang", "Credit / Utang Records", "bi-wallet2", "Customers",
                "All credit records with due dates and balances.",
                HasRange: true, DefaultRange: "all", HasCustomer: true,
                Statuses: new[] { "Unpaid", "Partial", "Paid", "Overdue" }),
        };

        public static ReportInfo Find(string? key) =>
            All.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase)) ?? All[0];

        /// Cleans the filter so only options this report supports survive.
        public static ReportInfo Normalize(ReportFilter f)
        {
            var info = Find(f.Type);
            f.Type = info.Key;
            f.Status = info.Statuses?.FirstOrDefault(s => s.Equals(f.Status, StringComparison.OrdinalIgnoreCase));

            if (!info.HasCategory) f.CategoryId = null;
            if (!info.HasCustomer) f.CustomerId = null;

            if (info.HasRange)
            {
                var r = ReportRanges.Resolve(f, info.DefaultRange);
                f.Range = r.Key;
                if (r.Key == "custom")
                {
                    f.From = r.Start;
                    f.To = r.EndExclusive!.Value.AddDays(-1);
                }
                else { f.From = null; f.To = null; }
            }
            else { f.Range = null; f.From = null; f.To = null; }

            return info;
        }
    }

    // ---------- date ranges ----------
    public record DateRange(DateTime? Start, DateTime? EndExclusive, string Key, string Label);

    public static class ReportRanges
    {
        public static readonly string[] Keys = { "today", "week", "month", "lastmonth", "year", "all", "custom" };
        public static readonly DateRange AllTime = new(null, null, "all", "All time");

        public static DateRange Resolve(ReportFilter f, string defaultKey)
        {
            var key = (f.Range ?? defaultKey).Trim().ToLowerInvariant();
            if (!Keys.Contains(key)) key = defaultKey;

            var today = DateTime.Today;

            switch (key)
            {
                case "today":
                    return Make("Today", key, today, today.AddDays(1));

                case "week":
                {
                    // weeks run Monday to Sunday
                    var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                    return Make("This week", key, monday, monday.AddDays(7));
                }

                case "month":
                {
                    var first = new DateTime(today.Year, today.Month, 1);
                    return Make("This month", key, first, first.AddMonths(1));
                }

                case "lastmonth":
                {
                    var first = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                    return Make("Last month", key, first, first.AddMonths(1));
                }

                case "year":
                {
                    var first = new DateTime(today.Year, 1, 1);
                    return Make("This year", key, first, first.AddYears(1));
                }

                case "custom":
                {
                    var start = (f.From ?? today).Date;
                    var end = (f.To ?? start).Date;
                    if (end < start) (start, end) = (end, start);
                    return Make("Custom", key, start, end.AddDays(1));
                }

                default:
                    return AllTime;
            }
        }

        private static DateRange Make(string label, string key, DateTime start, DateTime endExclusive) =>
            new(start, endExclusive, key, $"{label} ({Describe(start, endExclusive)})");

        private static string Describe(DateTime start, DateTime endExclusive)
        {
            var inv = CultureInfo.InvariantCulture;
            var last = endExclusive.AddDays(-1);

            if (start.Date == last.Date) return start.ToString("MMM d, yyyy", inv);
            if (start.Year == last.Year)
                return start.ToString("MMM d", inv) + " – " + last.ToString("MMM d, yyyy", inv);
            return start.ToString("MMM d, yyyy", inv) + " – " + last.ToString("MMM d, yyyy", inv);
        }
    }

    // ---------- formatting shared by the page and the PDF ----------
    public static class ReportFormat
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Money(decimal v, string peso = "₱") =>
            (v < 0 ? "-" : "") + peso + Math.Abs(v).ToString("N2", Inv);

        public static string Int(int v) => v.ToString("N0", Inv);

        public static string Cell(object? v, CellKind kind, string peso = "₱") => v switch
        {
            null => "",
            decimal d => Money(d, peso),
            int i => Int(i),
            DateTime dt => kind == CellKind.Date
                ? dt.ToString("MMM d, yyyy", Inv)
                : dt.ToString("MMM d, yyyy h:mm tt", Inv),
            _ => v.ToString() ?? ""
        };

        public static string StatusColor(string? status) => (status ?? "").ToLowerInvariant() switch
        {
            "paid" or "in stock" => "#3c9461",
            "partial" or "low stock" => "#9a6a0a",
            "unpaid" or "out of stock" or "overdue" => "#c0392b",
            _ => "#222222"
        };
    }

    // ---------- what the Reports page receives ----------
    public class ReportsPageVm
    {
        public ReportFilter Filter { get; set; } = new();
        public ReportInfo Info { get; set; } = ReportCatalog.All[0];
        public ReportTable? Table { get; set; }
        public OverviewVm? Overview { get; set; }
        public List<(int Id, string Name)> Categories { get; set; } = new();
        public List<(int Id, string Name)> Customers { get; set; } = new();
    }
}