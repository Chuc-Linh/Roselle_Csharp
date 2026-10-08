namespace Roselle.Web.Models
{
    public class Cart
    {
        public int CartID { get; set; }
        public int? UserID { get; set; }
        public string? SessionID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public List<CartItem> Items { get; set; } = new();
    }
}