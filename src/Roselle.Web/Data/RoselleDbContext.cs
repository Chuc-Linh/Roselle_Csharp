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
        public DbSet<CartItem> CartItems { get; set; }
        
        // Thêm 3 bảng mới cho phần Đơn hàng
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ánh xạ Product -> bảng PRODUCT
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("PRODUCT");
                entity.Property(e => e.SellingPrice)
                      .HasComputedColumnSql("CAST(CostPrice * (1 + ProfitRate/100.0) AS DECIMAL(18,2))", stored: false);
            });

            // Ánh xạ CartItem -> bảng SHOPPING_CART_ITEM
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("SHOPPING_CART_ITEM");
                entity.HasKey(e => new { e.CustomerID, e.ProductID });

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductID);
            });

            // Cấu hình khóa chính phức hợp cho OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .HasKey(od => new { od.OrderID, od.ProductID });
        }
    }
}