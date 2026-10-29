using System;

namespace QuanLyBanHoa.Desktop.Models
{
    public class SalesOrder
    {
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Today;
        public string? DeliveryAddress { get; set; }
        public string OrderStatus { get; set; } = "Mới";
    }
}