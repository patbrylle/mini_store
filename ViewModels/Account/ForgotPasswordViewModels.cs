using System.ComponentModel.DataAnnotations;
using mini_store.Validation;

namespace mini_store.ViewModels.Account
{
    public class SendResetCodeViewModel
    {
        [Required(ErrorMessage = "Enter your email address.")]
        [StringLength(100)]
        [StrongEmail]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetWithCodeViewModel
    {
        [Required, StringLength(100), StrongEmail]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the 6-digit code.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Enter the 6-digit code.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a new password.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm the new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}