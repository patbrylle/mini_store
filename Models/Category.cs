using System.ComponentModel.DataAnnotations;

namespace mini_store.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}