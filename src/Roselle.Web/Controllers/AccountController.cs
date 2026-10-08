using Microsoft.AspNetCore.Mvc;
using Roselle.Web.Models;
using Roselle.Web.Services;

namespace Roselle.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ICartService _cartService;

        // Inject CartService qua constructor
        public AccountController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // Tạm thời lưu user trong static list — sau này thay bằng database/API
        private static readonly List<User> MockUsers = new()
        {
            new User
            {
                UserID = 1,
                Username = "admin",
                PasswordHash = "admin123",
                FullName = "Quản trị viên",
                Email = "admin@roselle.example",
                Phone = "0123456789",
                Address = "123 Đường Hoa, Quận A, TP. HCM",
                Role = "Admin"
            },
            new User
            {
                UserID = 2,
                Username = "khach",
                PasswordHash = "khach123",
                FullName = "Khách hàng Demo",
                Email = "khach@roselle.example",
                Phone = "0987654321",
                Address = "456 Đường Lá, Quận B, TP. HCM",
                Role = "Customer"
            }
        };

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["Title"] = "Đăng nhập";
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            var user = MockUsers.FirstOrDefault(u =>
                u.Username == username && u.PasswordHash == password && u.IsActive);

            if (user == null)
            {
                ViewData["Error"] = "Tên đăng nhập hoặc mật khẩu không đúng.";
                ViewData["Title"] = "Đăng nhập";
                return View();
            }

            // Lưu session user
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("Username", user.Username);

            // === GỘP CART SESSION → CART USER ===
            var sessionCartId = HttpContext.Session.GetString("CartSessionId");
            if (!string.IsNullOrEmpty(sessionCartId))
            {
                await _cartService.MergeSessionCartToUserAsync(sessionCartId, user.UserID);
                HttpContext.Session.Remove("CartSessionId");
            }

            // Redirect
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            ViewData["Title"] = "Đăng ký";
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string confirmPassword,
            string fullName, string email, string phone, string address)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["Error"] = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewData["Error"] = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            if (MockUsers.Any(u => u.Username == username))
            {
                ViewData["Error"] = "Tên đăng nhập đã tồn tại.";
                return View();
            }

            var newUser = new User
            {
                UserID = MockUsers.Max(u => u.UserID) + 1,
                Username = username,
                PasswordHash = password,
                FullName = fullName,
                Email = email,
                Phone = phone,
                Address = address,
                Role = "Customer"
            };

            MockUsers.Add(newUser);

            // Tự động đăng nhập
            HttpContext.Session.SetInt32("UserID", newUser.UserID);
            HttpContext.Session.SetString("UserName", newUser.FullName);
            HttpContext.Session.SetString("UserRole", newUser.Role);
            HttpContext.Session.SetString("Username", newUser.Username);

            // === GỘP CART SESSION → CART USER ===
            var sessionCartId = HttpContext.Session.GetString("CartSessionId");
            if (!string.IsNullOrEmpty(sessionCartId))
            {
                await _cartService.MergeSessionCartToUserAsync(sessionCartId, newUser.UserID);
                HttpContext.Session.Remove("CartSessionId");
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            // Xóa session user nhưng giữ cart session (nếu muốn)
            HttpContext.Session.Remove("UserID");
            HttpContext.Session.Remove("UserName");
            HttpContext.Session.Remove("UserRole");
            HttpContext.Session.Remove("Username");

            return RedirectToAction("Index", "Home");
        }
    }
}