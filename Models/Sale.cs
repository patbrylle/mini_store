using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mini_store.Models
{
    public class Sale
    {
        public int SaleId { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public DateTime SaleDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal AmountPaid { get; set; }

        [StringLength(20)]
        public string PaymentMethod { get; set; } = "Cash";   // "Cash" or "Utang" (sale type)

        [StringLength(20)]
        public string Status { get; set; } = "Paid";

        [StringLength(40)]
        public string? IdempotencyKey { get; set; }

        [StringLength(256)]
        public string? CreatedBy { get; set; }

        [NotMapped]
        public string TransactionNo => $"TXN-{SaleDate:yyyyMMdd}-{SaleId:D5}";

        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}