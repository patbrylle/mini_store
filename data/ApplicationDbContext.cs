using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using mini_store.Models;

namespace mini_store.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Utang> Utangs { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);   // required for Identity

            builder.Entity<Sale>(e =>
            {
                e.HasIndex(s => s.IdempotencyKey).IsUnique();
                e.HasIndex(s => s.SaleDate);
            });

            builder.Entity<Payment>(e =>
            {
                e.HasIndex(p => new { p.SaleId, p.Sequence }).IsUnique();

                e.HasOne(p => p.Sale)
                 .WithMany(s => s.Payments)
                 .HasForeignKey(p => p.SaleId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(p => p.Utang)
                 .WithMany(u => u.Payments)
                 .HasForeignKey(p => p.UtangId)
                 .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}