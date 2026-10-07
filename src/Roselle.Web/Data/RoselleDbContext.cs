using Microsoft.EntityFrameworkCore;
using Roselle.Web.Models;

namespace Roselle.Web.Data
{
    public class RoselleDbContext : DbContext
    {
        public RoselleDbContext(DbContextOptions<RoselleDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mỗi User có 1 Cart
            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.UserID)
                .IsUnique()
                .HasFilter("[UserID] IS NOT NULL");

            // Mỗi Session có 1 Cart
            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.SessionID)
                .IsUnique()
                .HasFilter("[SessionID] IS NOT NULL");
        }
    }
}