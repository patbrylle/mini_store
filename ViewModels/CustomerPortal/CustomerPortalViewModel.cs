
using mini_store.Models;

namespace mini_store.ViewModels.CustomerPortal
{
    public class CustomerPortalViewModel
    {
        public string CustomerName { get; set; } = string.Empty;

        // Total value of recorded purchases
        public decimal TotalPurchases { get; set; }

        // Total money recorded as paid on sales
        public decimal TotalAmountPaid { get; set; }

        // Remaining unpaid utang
        public decimal TotalUtangBalance { get; set; }

        public List<Sale> Transactions { get; set; } = new();

        public List<Utang> Utangs { get; set; } = new();
    }
}
