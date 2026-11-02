using System;

namespace QuanLyBanHoa.Desktop.Models
{
    public class GoodsReceipt
    {
        public int ReceiptID { get; set; }
        public DateTime ImportDate { get; set; } = DateTime.Today;
        public string? Note { get; set; }
        public string Status { get; set; } = "Chưa hoàn thành";
    }
}