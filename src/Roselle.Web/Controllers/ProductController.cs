using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Roselle.Web.Data;

namespace Roselle.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly RoselleDbContext _db;

        public ProductController(RoselleDbContext db)
        {
            _db = db;
        }

        private static readonly Dictionary<string, (string Title, string Description)> Categories = new()
        {
            ["cam-tu"] = (
                "Hoa Cẩm Tú",
                "Bộ sưu tập Cẩm Tú Cầu — mix màu, hộp quà và thiết kế cao cấp."
            ),
            ["tulip"] = (
                "Hoa Tulip",
                "Bộ sưu tập hoa Tulip sang trọng với nhiều màu sắc như đỏ, hồng, vàng và trắng. Tulip tượng trưng cho tình yêu hoàn hảo, sự thanh lịch và may mắn, rất thích hợp làm quà tặng trong các dịp đặc biệt."
            ),
            ["hong"] = (
                "Hoa Hồng",
                "Bộ sưu tập hoa hồng — biểu tượng của tình yêu, sự lãng mạn và vẻ đẹp vĩnh cửu."
            ),
            ["huong-duong"] = (
                "Hoa Hướng Dương",
                "Bộ sưu tập hoa hướng dương rực rỡ — biểu tượng của niềm tin, hy vọng và sự lạc quan."
            ),
            ["baby"] = (
                "Hoa Baby",
                "Bộ sưu tập hoa Baby nhẹ nhàng, tinh khôi — loài hoa nhỏ xinh tượng trưng cho tình yêu thuần khiết."
            )
        };

        public async Task<IActionResult> Category(string id)
        {
            var (title, description) = Categories.TryGetValue(id ?? "", out var info)
                ? info
                : ("Danh mục sản phẩm", "Bộ sưu tập hoa tươi cho mọi dịp.");

            ViewData["Title"] = title;
            ViewData["CategoryId"] = id;
            ViewData["CategoryTitle"] = title;
            ViewData["CategoryDescription"] = description;

            var products = await _db.Products
                .Where(p => p.CategoryID == id)
                .ToListAsync();

            return View("Index", products);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewData["Title"] = product.ProductName;
            return View(product);
        }

        public async Task<IActionResult> Search(string q)
        {
            ViewData["Title"] = "Tìm kiếm: " + q;
            ViewData["Query"] = q;

            var products = await _db.Products
                .Where(p => p.ProductName.Contains(q ?? ""))
                .ToListAsync();

            return View("Search", products);
        }
    }
}