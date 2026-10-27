using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Roselle.Web.Data;
using Roselle.Web.Models;
using Roselle.Web.Helpers;

namespace Roselle.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly RoselleDbContext _db;

        public ProductController(RoselleDbContext db)
        {
            _db = db;
        }

        // Map slug trên URL -> ProductTypeID trong DB
        private static readonly Dictionary<string, int> CategoryTypeMap = new()
        {
            ["cam-tu"] = 1,       // Hoa Cẩm Tú
            ["tulip"] = 2,        // Hoa Tulip
            ["hong"] = 3,         // Hoa hồng
            ["huong-duong"] = 4,  // Hoa hướng dương
            ["baby"] = 5,         // Hoa Baby
            ["dong-tien"] = 6,    // Hoa Đồng Tiền
            ["ly"] = 7,           // Hoa ly
            ["lan-ho-diep"] = 8   // Lan Hồ Điệp
        };

        private static readonly Dictionary<string, (string Title, string Description)> Categories = new()
        {
            ["cam-tu"] = ("Hoa Cẩm Tú", "Bộ sưu tập Cẩm Tú Cầu — mix màu, hộp quà và thiết kế cao cấp."),
            ["tulip"] = ("Hoa Tulip", "Bộ sưu tập hoa Tulip sang trọng với nhiều màu sắc như đỏ, hồng, vàng và trắng."),
            ["hong"] = ("Hoa Hồng", "Bộ sưu tập hoa hồng — biểu tượng của tình yêu, sự lãng mạn và vẻ đẹp vĩnh cửu."),
            ["huong-duong"] = ("Hoa Hướng Dương", "Bộ sưu tập hoa hướng dương rực rỡ — biểu tượng của niềm tin, hy vọng."),
            ["baby"] = ("Hoa Baby", "Bộ sưu tập hoa Baby nhẹ nhàng, tinh khôi — tượng trưng cho tình yêu thuần khiết."),
            ["dong-tien"] = ("Hoa Đồng Tiền", "Bộ sưu tập hoa Đồng Tiền — may mắn và tài lộc."),
            ["ly"] = ("Hoa Ly", "Bộ sưu tập hoa Ly thơm ngát, thanh tao."),
            ["lan-ho-diep"] = ("Lan Hồ Điệp", "Bộ sưu tập Lan Hồ Điệp sang trọng, quý phái.")
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

            if (!CategoryTypeMap.TryGetValue(id ?? "", out var typeId))
            {
                return View("Index", new List<Product>());
            }

            var products = await _db.Products
                .Where(p => p.ProductTypeID == typeId)
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

        // Hàm tính khoảng cách Levenshtein (để tìm kiếm mờ)
        private int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        // Action Tìm kiếm chính (Đã nâng cấp Tìm kiếm mờ)
        public async Task<IActionResult> Search(string q)
        {
            ViewData["Title"] = "Tìm kiếm: " + q;
            ViewData["Query"] = q;

            if (string.IsNullOrWhiteSpace(q))
            {
                return View("Search", new List<Product>());
            }

            var allProducts = await _db.Products.ToListAsync();
            var searchNormalized = StringHelper.RemoveDiacritics(q).Trim();
            var categoryNames = new Dictionary<int, string> { { 1, "hoa cam tu" }, { 2, "hoa tulip" }, { 3, "hoa hong" }, { 4, "hoa huong duong" }, { 5, "hoa baby" } };

            var products = allProducts.Where(p => 
            {
                var name = StringHelper.RemoveDiacritics(p.ProductName);
                var desc = StringHelper.RemoveDiacritics(p.Description ?? "");
                string catName = categoryNames.ContainsKey(p.ProductTypeID) ? categoryNames[p.ProductTypeID] : "";

                if (name.Contains(searchNormalized) || desc.Contains(searchNormalized) || catName.Contains(searchNormalized)) 
                    return true;

                int tolerance = searchNormalized.Length > 5 ? 2 : (searchNormalized.Length > 3 ? 1 : 0);
                
                if (tolerance > 0)
                {
                    if (catName.Length > 0 && LevenshteinDistance(searchNormalized, catName) <= tolerance) return true;
                    if (LevenshteinDistance(searchNormalized, name) <= tolerance) return true;
                    
                    var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        if (word.Length > 2 && LevenshteinDistance(searchNormalized, word) <= tolerance) return true;
                    }
                }

                return false;
            }).ToList();

            return View("Search", products);
        }

        // Action Gợi ý tìm kiếm (Auto-complete)
        [HttpGet]
        public async Task<IActionResult> SearchSuggest(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2) 
                return Json(new List<string>());

            var searchNormalized = StringHelper.RemoveDiacritics(term).Trim();
            
            var allProducts = await _db.Products.ToListAsync();
            var suggestions = allProducts
                .Where(p => StringHelper.RemoveDiacritics(p.ProductName).Contains(searchNormalized))
                .Select(p => p.ProductName)
                .Take(5)
                .ToList();

            return Json(suggestions);
        }
    }
}