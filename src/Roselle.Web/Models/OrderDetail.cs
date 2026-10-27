using System.ComponentModel.DataAnnotations.Schema;

namespace Roselle.Web.Models
{
    [Table("ORDER_DETAIL")]
    public class OrderDetail
    {
        [Column("OrderID")]
        public int OrderID { get; set; }

        [Column("ProductID")]
        public int ProductID { get; set; }

        [Column("Quantity")]
        public int Quantity { get; set; }

        [Column("UnitPrice")]
        public decimal UnitPrice { get; set; }

        [Column("CostPrice")]
        public decimal CostPrice { get; set; }

        [Column("Discount")]
        public decimal Discount { get; set; }
    }
}