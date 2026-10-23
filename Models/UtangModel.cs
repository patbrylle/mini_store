using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mini_store.Models
{
    public class Utang
    {
        public int UtangId { get; set; }

        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public int SaleId { get; set; }
        public Sale? Sale { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        // Concurrency token: two simultaneous payments can't both succeed.
        [ConcurrencyCheck]
        [Column(TypeName = "decimal(10,2)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Balance { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public DateTime? DueDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Unpaid";

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        // single source of truth for "overdue"
[NotMapped]
public bool IsOverdue =>
    Balance > 0 && DueDate.HasValue && DueDate.Value.Date < DateTime.Today;
    }
}