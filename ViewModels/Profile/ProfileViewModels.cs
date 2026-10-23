using System.ComponentModel.DataAnnotations;
using mini_store.Validation;
using mini_store.ViewModels.Account;

namespace mini_store.ViewModels.Profile
{
    public class ProfilePageViewModel
    {
        public string RoleLabel { get; set; } = "User";
        public bool IsCustomer { get; set; }
        public bool HasCustomerRecord { get; set; }
        public string DisplayName { get; set; } = "User";

        // bound with Prefix = "Info" in UpdateProfile
        public UpdateProfileViewModel Info { get; set; } = new();

        // bound with Prefix = "Password" in ChangePassword
        public ChangePasswordViewModel Password { get; set; } = new();
    }

    public class UpdateProfileViewModel
    {
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [StringLength(100)]
        [StrongEmail]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Contact number is too long.")]
        [PhPhone]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        [SafeAddress]
        public string? Address { get; set; }
    }
}