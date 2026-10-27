using Microsoft.AspNetCore.Mvc;
using Roselle.Web.Data;
using Roselle.Web.Models;
using Roselle.Web.Models.ViewModels;
using Roselle.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace Roselle.Web.Controllers
{
    public class OrderController : Controller
    {
        private readonly RoselleDbContext _db;
        private readonly ICartService _cartService;

        public OrderController(RoselleDbContext db, ICartService cartService)
        {
            _db = db;
            _cartService = cartService;
        }

        private int? CurrentCustomerId => HttpContext.Session.GetInt32("UserID");

        // GET: /Order/Checkout
        public async Task<IActionResult> Checkout()
        {
            // 1. Kiểm tra đăng nhập (Bắt buộc phải đăng nhập mới được thanh toán)
            if (CurrentCustomerId == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Order/Checkout" });
            }

            // 2. Lấy giỏ hàng từ DB
            var cart = await _cartService.GetCartAsync(CurrentCustomerId.Value);
            if (cart.Items == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart"); // Giỏ hàng rỗng thì về lại giỏ
            }

            // 3. Truyền thông tin user vào View để tự động điền form
            ViewData["Title"] = "Thanh toán";
            ViewData["FullName"] = HttpContext.Session.GetString("UserName") ?? "";
            ViewData["Phone"] = HttpContext.Session.GetString("UserPhone") ?? "";
            ViewData["Address"] = HttpContext.Session.GetString("UserAddress") ?? "";

            return View(cart);
        }

        // POST: /Order/PlaceOrder
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string fullName, string phone, string address, string paymentMethod)
        {
            if (CurrentCustomerId == null) return RedirectToAction("Login", "Account");

            var cart = await _cartService.GetCartAsync(CurrentCustomerId.Value);
            if (cart.Items == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Bắt đầu lưu vào Database (Dùng Transaction để đảm bảo toàn vẹn dữ liệu)
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. Tạo Đơn hàng (SALES_ORDER)
                var order = new SalesOrder
                {
                    CustomerID = CurrentCustomerId.Value,
                    CreatedDate = DateTime.Now,
                    DeliveryAddress = address,
                    OrderStatus = "Mới" // Trigger của Bảo sẽ tự trừ kho khi đổi sang "Đang xử lý"
                };
                _db.SalesOrders.Add(order);
                await _db.SaveChangesAsync(); // Lưu để lấy OrderID

                // 2. Lưu Chi tiết đơn hàng (ORDER_DETAIL)
                foreach (var item in cart.Items)
                {
                    var detail = new OrderDetail
                    {
                        OrderID = order.OrderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product?.SellingPrice ?? 0,
                        CostPrice = item.Product?.CostPrice ?? 0,
                        Discount = 0
                    };
                    _db.OrderDetails.Add(detail);
                }

                // 3. Lưu Thanh toán (PAYMENT)
                var payment = new Payment
                {
                    OrderID = order.OrderID,
                    Method = paymentMethod == "COD" ? "Tiền mặt" : "Chuyển khoản",
                    Status = paymentMethod == "COD" ? "Chưa thanh toán" : "Đã thanh toán",
                    PaymentDate = paymentMethod == "COD" ? null : DateTime.Now
                };
                _db.Payments.Add(payment);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync(); // Hoàn tất giao dịch

                // 4. Xóa giỏ hàng sau khi đặt thành công
                await _cartService.ClearCartAsync(CurrentCustomerId.Value);

                TempData["Success"] = "Đặt hàng thành công! Chúng tôi sẽ sớm liên hệ giao hoa.";
                return RedirectToAction("OrderSuccess");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Ghi log lỗi (nếu cần)
                ViewData["Error"] = "Có lỗi xảy ra trong quá trình đặt hàng. Vui lòng thử lại sau.";
                
                // Load lại thông tin để hiển thị lại form
                ViewData["Title"] = "Thanh toán";
                ViewData["FullName"] = fullName;
                ViewData["Phone"] = phone;
                ViewData["Address"] = address;
                return View("Checkout", cart);
            }
        }

        public IActionResult OrderSuccess()
        {
            return View();
        }
    }
}