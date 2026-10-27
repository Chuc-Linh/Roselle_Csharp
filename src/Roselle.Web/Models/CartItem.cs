using System.ComponentModel.DataAnnotations.Schema;

namespace Roselle.Web.Models
{
    [Table("SHOPPING_CART_ITEM")]
    public class CartItem
    {
        public int CustomerID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }

        [ForeignKey("ProductID")]
        public Product? Product { get; set; }
    }
}
