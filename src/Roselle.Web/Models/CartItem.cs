using System.ComponentModel.DataAnnotations.Schema;

namespace Roselle.Web.Models
{
    public class CartItem
    {
        public int CartItemID { get; set; }
        public int CartID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Navigation
        public Cart? Cart { get; set; }
        public Product? Product { get; set; }

        public decimal SubTotal => Quantity * UnitPrice;
    }
}