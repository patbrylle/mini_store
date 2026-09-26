using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mini_store.Models
{
    public class Utang
    {
        public int UtangId { get; set; }

        // Customer who owes the money
        public int CustomerId { get; set; }

        public Customer? Customer { get; set; }

        // Sale connected to this utang
        public int SaleId { get; set; }

        public Sale? Sale { get; set; }

        // Original amount of the sale
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        // Amount already paid
        [Column(TypeName = "decimal(10,2)")]
        public decimal AmountPaid { get; set; }

        // Remaining balance
        [Column(TypeName = "decimal(10,2)")]
        public decimal Balance { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string Status { get; set; } = "Unpaid";
    }
}