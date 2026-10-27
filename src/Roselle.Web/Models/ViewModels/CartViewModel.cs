namespace Roselle.Web.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartItem> Items { get; set; } = new();

        public decimal TotalAmount => Items.Sum(i =>
            (i.Product?.SellingPrice ?? 0m) * i.Quantity);

        public int TotalQuantity => Items.Sum(i => i.Quantity);
    }
}