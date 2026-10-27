using Microsoft.AspNetCore.Mvc;
using Roselle.Web.Services;
using Roselle.Web.Models;
using Roselle.Web.Models.ViewModels;
using Roselle.Web.Data;

namespace Roselle.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly RoselleDbContext _db; // Dùng để lấy thông tin sản phẩm cho khách vãng lai

        public CartController(ICartService cartService, RoselleDbContext db)
        {
            _cartService = cartService;
            _db = db;
        }

        private int? CurrentCustomerId => HttpContext.Session.GetInt32("UserID");

        // Xem giỏ hàng
        public async Task<IActionResult> Index()
        {
            if (CurrentCustomerId == null)
            {
                // Khách vãng lai: Lấy từ Session
                var guestItems = _cartService.GetGuestCart(HttpContext.Session);
                
                // Cần lấy thông tin chi tiết (tên, ảnh, giá) của các sản phẩm trong giỏ
                foreach (var item in guestItems)
                {
                    item.Product = await _db.Products.FindAsync(item.ProductID);
                }

                return View(new CartViewModel { Items = guestItems });
            }

            // Thành viên: Lấy từ Database
            var vm = await _cartService.GetCartAsync(CurrentCustomerId.Value);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            if (CurrentCustomerId == null)
            {
                // Chưa đăng nhập -> Thêm vào Session
                _cartService.AddToGuestCart(HttpContext.Session, productId, quantity);
                var count = _cartService.GetGuestCart(HttpContext.Session).Sum(c => c.Quantity);
                return Json(new { success = true, cartCount = count });
            }

            // Đã đăng nhập -> Thêm vào DB
            var countDb = await _cartService.AddToCartAsync(CurrentCustomerId.Value, productId, quantity);
            return Json(new { success = true, cartCount = countDb });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            if (CurrentCustomerId == null)
            {
                _cartService.UpdateGuestCart(HttpContext.Session, productId, quantity);
                
                // Tính lại tổng tiền cho Guest để trả về
                var guestCart = _cartService.GetGuestCart(HttpContext.Session);
                decimal totalAmount = 0;
                foreach(var item in guestCart) {
                    var p = await _db.Products.FindAsync(item.ProductID);
                    if(p != null) totalAmount += (p.SellingPrice ?? 0) * item.Quantity;
                }
                return Json(new { success = true, totalAmount = totalAmount, totalQuantity = guestCart.Sum(c => c.Quantity) });
            }

            await _cartService.UpdateQuantityAsync(CurrentCustomerId.Value, productId, quantity);
            var vm = await _cartService.GetCartAsync(CurrentCustomerId.Value);
            return Json(new { success = true, totalAmount = vm.TotalAmount, totalQuantity = vm.TotalQuantity });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            if (CurrentCustomerId == null)
            {
                _cartService.RemoveFromGuestCart(HttpContext.Session, productId);
                
                var guestCart = _cartService.GetGuestCart(HttpContext.Session);
                decimal totalAmount = 0;
                foreach(var item in guestCart) {
                    var p = await _db.Products.FindAsync(item.ProductID);
                    if(p != null) totalAmount += (p.SellingPrice ?? 0) * item.Quantity;
                }
                return Json(new { success = true, totalAmount = totalAmount, totalQuantity = guestCart.Sum(c => c.Quantity) });
            }

            await _cartService.RemoveItemAsync(CurrentCustomerId.Value, productId);
            var vm = await _cartService.GetCartAsync(CurrentCustomerId.Value);
            return Json(new { success = true, totalAmount = vm.TotalAmount, totalQuantity = vm.TotalQuantity });
        }
    }
}