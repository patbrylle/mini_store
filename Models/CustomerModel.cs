using System.ComponentModel.DataAnnotations;
using mini_store.Validation;


namespace mini_store.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Customer name is required.")]
[StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2 to 100 characters.")]
[PersonName]
        public string CustomerName { get; set; } = string.Empty;

[StringLength(100)]
[StrongEmail]
        public string? Email { get; set; }

[StringLength(20, ErrorMessage = "Contact number is too long.")]
[PhPhone]
        public string? ContactNumber { get; set; }

[StringLength(200)]
[SafeAddress]
        public string? Address { get; set; }

                public string? UserId { get; set; }

    }
}