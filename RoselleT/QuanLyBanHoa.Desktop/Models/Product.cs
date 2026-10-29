namespace QuanLyBanHoa.Desktop.Models
{
    public class Product
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int ProductTypeID { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal ProfitRate { get; set; }
        public decimal SellingPrice { get; private set; } // Cột AS tự tính trong SQL
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; } = 5;
        public string? Image { get; set; }
        public string? SupplierName { get; set; }
        public bool Status { get; set; } = true;
    }
}