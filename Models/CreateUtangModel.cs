using System.ComponentModel.DataAnnotations;

namespace mini_store.Models
{
    public class CreateUtang
    {
        [Required]
        public int CustomerId { get; set; }

        public List<UtangProductViewModel> Products { get; set; } = new();

        [Range(0, double.MaxValue)]
        public decimal AmountPaid { get; set; }
    }

    public class UtangProductViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Quantity { get; set; }
    }
}