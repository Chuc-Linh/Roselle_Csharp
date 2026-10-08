using Roselle.Web.Models;

namespace Roselle.Web.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartItem> Items { get; set; } = new();
        public decimal TotalAmount => Items.Sum(i => i.SubTotal);
        public int TotalQuantity => Items.Sum(i => i.Quantity);
    }
}