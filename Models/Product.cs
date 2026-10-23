using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mini_store.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        [Required]
        public string? ProductName { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}