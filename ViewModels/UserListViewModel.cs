namespace mini_store.ViewModels.Account
{
    public class UserListViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;   // customer name, otherwise the email
        public string Role { get; set; } = string.Empty;          // Admin | Staff | Customer
        public string Status { get; set; } = "active";            // active | locked | disabled
    }
}