using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roselle.Web.Models
{
    [Table("SALES_ORDER")]
    public class SalesOrder
    {
        [Key] // BẮT BUỘC PHẢI CÓ DÒNG NÀY
        [Column("OrderID")]
        public int OrderID { get; set; }

        [Column("CustomerID")]
        public int CustomerID { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [Column("DeliveryAddress")]
        public string? DeliveryAddress { get; set; }

        [Column("OrderStatus")]
        public string OrderStatus { get; set; } = "Mới";
    }
}