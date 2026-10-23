using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mini_store.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        public int SaleId { get; set; }
        public Sale? Sale { get; set; }

        public int? UtangId { get; set; }
        public Utang? Utang { get; set; }

        // 1 = payment made at the time of sale, 2+ = later utang payments
        public int Sequence { get; set; } = 1;

        // Amount applied to the sale (never includes change)
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal AmountTendered { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ChangeGiven { get; set; }

        [StringLength(20)]
        public string Method { get; set; } = "Cash";

        [StringLength(60)]
        public string? ReferenceNo { get; set; }

        [StringLength(256)]
        public string? ReceivedBy { get; set; }

        public DateTime PaidAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string ReceiptNo => $"OR-{PaidAt:yyyyMMdd}-{SaleId:D5}-{Sequence:D2}";
    }
}