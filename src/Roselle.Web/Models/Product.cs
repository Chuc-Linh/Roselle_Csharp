using System.ComponentModel.DataAnnotations.Schema;

namespace Roselle.Web.Models
{
    public class Product
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public string? CategoryID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Stock { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public bool IsHot { get; set; }
    }
}