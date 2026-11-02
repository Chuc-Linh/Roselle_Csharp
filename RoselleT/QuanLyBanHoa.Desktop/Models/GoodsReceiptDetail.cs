namespace QuanLyBanHoa.Desktop.Models
{
    public class GoodsReceiptDetail
    {
        public int ReceiptID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal ImportPrice { get; set; }
        public decimal Amount { get; private set; } // Cột AS tự tính trong SQL
    }
}