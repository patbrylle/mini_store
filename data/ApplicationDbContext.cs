using Microsoft.EntityFrameworkCore;
using mini_store.Models;

namespace mini_store.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Category> Categories { get; set; } 
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }

    }
}