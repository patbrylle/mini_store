namespace mini_store.Models
{
    public class CustomerHistoryVm
    {
        public Customer Customer { get; set; } = null!;
        public List<Sale> Sales { get; set; } = new();
        public List<Utang> Utangs { get; set; } = new();

        public decimal TotalPurchases => Sales.Sum(s => s.TotalAmount);
        public decimal Outstanding => Utangs.Sum(u => u.Balance);
        public int OverdueCount => Utangs.Count(u => u.IsOverdue);
    }
}