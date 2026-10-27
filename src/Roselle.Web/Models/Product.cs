using System.ComponentModel.DataAnnotations.Schema;

namespace Roselle.Web.Models
{
    [Table("PRODUCT")]
    public class Product
    {
        public int ProductID { get; set; }
        public string? ProductCode { get; set; }
        public string ProductName { get; set; } = "";
        public int ProductTypeID { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string Unit { get; set; } = "";

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal ProfitRate { get; set; }

        // Computed column trong SQL → chỉ đọc
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? SellingPrice { get; private set; }

        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public string? Image { get; set; }
        public string? SupplierName { get; set; }
        public bool Status { get; set; }
    }
}