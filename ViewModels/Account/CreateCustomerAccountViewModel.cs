
using System.ComponentModel.DataAnnotations;

namespace mini_store.ViewModels.Account
{
    public class CreateCustomerAccountViewModel
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
