using System.ComponentModel.DataAnnotations;    
using System.ComponentModel.DataAnnotations.Schema; 

namespace mini_store.Models
{
    public class Product
    {
        public int ProductId{get; set; }

        [Required]
        public String? ProductName { get; set; } 

        [Required]
        public Decimal Price { get; set; }

        [Required]
        public int Quantity { get; set; }
        
        public int CategoryId { get; set; } 

        public Category? Category { get; set; }

    }
}