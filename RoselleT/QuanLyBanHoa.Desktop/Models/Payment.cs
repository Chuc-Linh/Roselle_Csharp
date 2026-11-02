using System;

namespace QuanLyBanHoa.Desktop.Models
{
    public class Payment
    {
        public int PaymentID { get; set; }
        public int OrderID { get; set; }
        public string Method { get; set; } = "Tiền mặt";
        public string Status { get; set; } = "Chưa thanh toán";
        public DateTime? PaymentDate { get; set; }
    }
}