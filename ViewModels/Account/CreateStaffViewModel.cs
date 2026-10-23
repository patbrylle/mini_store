using System.ComponentModel.DataAnnotations;
using mini_store.Validation;

namespace mini_store.ViewModels.Account
{
    public class CreateStaffViewModel
    {
        [Required]
[StringLength(100)]
[StrongEmail]
public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}