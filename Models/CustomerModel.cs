using System.ComponentModel.DataAnnotations;

namespace mini_store.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? ContactNumber { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }
    }
}