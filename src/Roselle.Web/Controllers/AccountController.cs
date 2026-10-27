using Microsoft.AspNetCore.Mvc;
using Roselle.Web.Models;
using Roselle.Web.Services;

namespace Roselle.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ICartService _cartService;

        public AccountController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private static readonly List<User> MockUsers = new()
        {
            new User { UserID = 1, Username = "admin", PasswordHash = "admin123", FullName = "Quản trị viên", Email = "admin@roselle.example", Phone = "0123456789", Address = "123 Đường Hoa, Quận A, TP. HCM", Role = "Admin" },
            new User { UserID = 2, Username = "khach", PasswordHash = "khach123", FullName = "Khách hàng Demo", Email = "khach@roselle.example", Phone = "0987654321", Address = "456 Đường Lá, Quận B, TP. HCM", Role = "Customer" }
        };

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["Title"] = "Đăng nhập";
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            var user = MockUsers.FirstOrDefault(u => u.Username == username && u.PasswordHash == password && u.IsActive);

            if (user == null)
            {
                ViewData["Error"] = "Tên đăng nhập hoặc mật khẩu không đúng.";
                ViewData["Title"] = "Đăng nhập";
                return View();
            }

            // 1. Lưu thông tin user vào Session
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserEmail", user.Email ?? "");
            HttpContext.Session.SetString("UserPhone", user.Phone ?? "");
            HttpContext.Session.SetString("UserAddress", user.Address ?? "");

            // 2. GỘP GIỎ HÀNG TẠM VÀO TÀI KHOẢN
            // (Hàm này sẽ lấy giỏ từ Session "GuestCart", lưu vào DB theo user.UserID, và xóa Session "GuestCart")
            await _cartService.MergeGuestCartToUserAsync(HttpContext.Session, user.UserID);

            // 3. Chuyển hướng
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewData["Title"] = "Đăng ký";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string confirmPassword, string fullName, string email, string phone, string address)
        {
            // ... (phần validate giữ nguyên như cũ) ...
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName))
            {
                ViewData["Error"] = "Vui lòng nhập đầy đủ thông tin bắt buộc (*).";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewData["Error"] = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            if (MockUsers.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                ViewData["Error"] = "Tên đăng nhập này đã tồn tại.";
                return View();
            }

            if (!string.IsNullOrWhiteSpace(email) && MockUsers.Any(u => !string.IsNullOrWhiteSpace(u.Email) && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                ViewData["Error"] = "Email này đã được sử dụng cho một tài khoản khác.";
                return View();
            }

            if (!string.IsNullOrWhiteSpace(phone) && MockUsers.Any(u => !string.IsNullOrWhiteSpace(u.Phone) && u.Phone == phone))
            {
                ViewData["Error"] = "Số điện thoại này đã được sử dụng cho một tài khoản khác.";
                return View();
            }

            // Tạo user mới
            var newUser = new User
            {
                UserID = MockUsers.Any() ? MockUsers.Max(u => u.UserID) + 1 : 1,
                Username = username,
                PasswordHash = password,
                FullName = fullName,
                Email = email,
                Phone = phone,
                Address = address,
                Role = "Customer"
            };

            MockUsers.Add(newUser);

            // 1. Lưu thông tin user mới vào Session
            HttpContext.Session.SetInt32("UserID", newUser.UserID);
            HttpContext.Session.SetString("UserName", newUser.FullName);
            HttpContext.Session.SetString("UserRole", newUser.Role);
            HttpContext.Session.SetString("Username", newUser.Username);
            HttpContext.Session.SetString("UserEmail", newUser.Email ?? "");
            HttpContext.Session.SetString("UserPhone", newUser.Phone ?? "");
            HttpContext.Session.SetString("UserAddress", newUser.Address ?? "");

            // 2. GỘP GIỎ HÀNG TẠM (nếu khách đã thêm hoa trước khi đăng ký)
            await _cartService.MergeGuestCartToUserAsync(HttpContext.Session, newUser.UserID);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = MockUsers.FirstOrDefault(u => u.UserID == userId);
            if (user == null) return NotFound();

            ViewData["Title"] = "Thông tin cá nhân";
            ViewData["Username"] = user.Username;
            ViewData["FullName"] = user.FullName;
            ViewData["Email"] = user.Email;
            ViewData["Phone"] = user.Phone;
            ViewData["Address"] = user.Address;
            ViewData["Role"] = user.Role;

            return View();
        }

        [HttpPost]
        public IActionResult UpdateProfile(string fullName, string email, string phone, string address)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = MockUsers.FirstOrDefault(u => u.UserID == userId);
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(email) && MockUsers.Any(u => u.UserID != userId && !string.IsNullOrWhiteSpace(u.Email) && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = "Email này đã được sử dụng cho một tài khoản khác.";
                return RedirectToAction("Profile");
            }

            if (!string.IsNullOrWhiteSpace(phone) && MockUsers.Any(u => u.UserID != userId && !string.IsNullOrWhiteSpace(u.Phone) && u.Phone == phone))
            {
                TempData["Error"] = "Số điện thoại này đã được sử dụng cho một tài khoản khác.";
                return RedirectToAction("Profile");
            }

            user.FullName = fullName;
            user.Email = email;
            user.Phone = phone;
            user.Address = address;

            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserEmail", user.Email ?? "");
            HttpContext.Session.SetString("UserPhone", user.Phone ?? "");
            HttpContext.Session.SetString("UserAddress", user.Address ?? "");

            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }

        public IActionResult Logout()
        {
            // Chỉ xóa session user, KHÔNG xóa "GuestCart" (để nếu họ thêm hoa lúc chưa login thì vẫn còn)
            HttpContext.Session.Remove("UserID");
            HttpContext.Session.Remove("UserName");
            HttpContext.Session.Remove("UserRole");
            HttpContext.Session.Remove("Username");
            HttpContext.Session.Remove("UserEmail");
            HttpContext.Session.Remove("UserPhone");
            HttpContext.Session.Remove("UserAddress");

            return RedirectToAction("Index", "Home");
        }
    }
}