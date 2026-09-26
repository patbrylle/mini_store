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
        public string PaymentMethod { get; set; } = "Cash";

        [StringLength(20)]
        public string Status { get; set; } = "Paid";

        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
}