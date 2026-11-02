using Microsoft.EntityFrameworkCore;
using QuanLyBanHoa.Desktop.Models;

namespace QuanLyBanHoa.Desktop.Data
{
    public class ShopDbContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
        public DbSet<GoodsReceiptDetail> GoodsReceiptDetails { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Nếu máy bạn dùng tên instance như .\SQLEXPRESS thì đổi localhost thành .\SQLEXPRESS
                optionsBuilder.UseSqlServer("Server=localhost;Database=QuanLyBanHoa;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. ACCOUNT
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("ACCOUNT");
                entity.HasKey(e => e.AccountID);
            });

            // 2. CUSTOMER
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("CUSTOMER");
                entity.HasKey(e => e.CustomerID);
            });

            // 3. PRODUCT_TYPE
            modelBuilder.Entity<ProductType>(entity =>
            {
                entity.ToTable("PRODUCT_TYPE");
                entity.HasKey(e => e.ProductTypeID);
            });

            // 4. PRODUCT (Có cột tính toán SellingPrice)
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("PRODUCT");
                entity.HasKey(e => e.ProductID);
                entity.Property(e => e.CostPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ProfitRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.SellingPrice)
                      .HasColumnType("decimal(18,2)")
                      .ValueGeneratedOnAddOrUpdate();
            });

            // 5. GOODS_RECEIPT (Có Trigger trong SQL)
            modelBuilder.Entity<GoodsReceipt>(entity =>
            {
                entity.ToTable("GOODS_RECEIPT", tb =>
                {
                    tb.HasTrigger("trg_GoodsReceipt_Complete");
                    tb.HasTrigger("trg_GoodsReceipt_BlockUpdate");
                });
                entity.HasKey(e => e.ReceiptID);
            });

            // 6. GOODS_RECEIPT_DETAIL (Khóa chính ghép + Cột Amount tự tính + Trigger)
            modelBuilder.Entity<GoodsReceiptDetail>(entity =>
            {
                entity.ToTable("GOODS_RECEIPT_DETAIL", tb => tb.HasTrigger("trg_GoodsReceiptDetail_Adjust"));
                entity.HasKey(e => new { e.ReceiptID, e.ProductID });
                entity.Property(e => e.ImportPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Amount)
                      .HasColumnType("decimal(18,2)")
                      .ValueGeneratedOnAddOrUpdate();
            });

            // 7. SALES_ORDER (Có Trigger trong SQL)
            modelBuilder.Entity<SalesOrder>(entity =>
            {
                entity.ToTable("SALES_ORDER", tb =>
                {
                    tb.HasTrigger("trg_SalesOrder_Stock");
                    tb.HasTrigger("trg_SalesOrder_BlockBackwardStatus");
                });
                entity.HasKey(e => e.OrderID);
            });

            // 8. ORDER_DETAIL (Khóa chính ghép + Cột Amount tự tính + Trigger)
            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.ToTable("ORDER_DETAIL", tb => tb.HasTrigger("trg_OrderDetail_CheckStock"));
                entity.HasKey(e => new { e.OrderID, e.ProductID });
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CostPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Discount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Amount)
                      .HasColumnType("decimal(18,2)")
                      .ValueGeneratedOnAddOrUpdate();
            });

            // 9. SHOPPING_CART_ITEM
            modelBuilder.Entity<ShoppingCartItem>(entity =>
            {
                entity.ToTable("SHOPPING_CART_ITEM");
                entity.HasKey(e => new { e.CustomerID, e.ProductID });
            });

            // 10. PAYMENT
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("PAYMENT");
                entity.HasKey(e => e.PaymentID);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}