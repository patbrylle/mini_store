namespace mini_store.Models
{
    public static class PaymentMethods
    {
        public static readonly string[] All = { "Cash", "GCash", "Maya", "Bank Transfer" };
    }

    public static class TxStatus
    {
        public static string From(decimal total, decimal paid) =>
            paid >= total ? "Paid" : paid > 0 ? "Partial" : "Unpaid";

        public static string Key(string? status) => status?.ToLowerInvariant() switch
        {
            "paid" => "paid",
            "partial" => "partial",
            _ => "unpaid"
        };
    }

    public class CreateSaleRequest
    {
        public string? TransactionType { get; set; }
        public int? CustomerId { get; set; }
        public int[]? ProductId { get; set; }
        public int[]? Quantity { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime? DueDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ReferenceNo { get; set; }
        public string? IdempotencyKey { get; set; }
    }

    public class SaleResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int SaleId { get; set; }
        public int? PaymentId { get; set; }
    }

    public class TransactionFormData
    {
        public List<Customer> Customers { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }

    public class TransactionVm
    {
        public Sale Sale { get; set; } = null!;
        public Utang? Utang { get; set; }
        public List<Payment> Payments { get; set; } = new();

        public decimal Balance => Math.Max(0, Sale.TotalAmount - Sale.AmountPaid);

        public bool IsOverdue =>
            Balance > 0 && Utang?.DueDate is DateTime d && d.Date < DateTime.Today;
    }
}