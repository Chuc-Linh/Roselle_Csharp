using Microsoft.AspNetCore.Mvc;
using Roselle.Web.Services;

namespace Roselle.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserID");

        private string GetOrCreateSessionId()
        {
            var sid = HttpContext.Session.GetString("CartSessionId");
            if (string.IsNullOrEmpty(sid))
            {
                sid = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("CartSessionId", sid);
            }
            return sid;
        }

        // GET: /Cart
        public async Task<IActionResult> Index()
        {
            var vm = await _cartService.GetCartAsync(CurrentUserId, GetOrCreateSessionId());
            return View(vm);
        }

        // POST: /Cart/Add
        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var count = await _cartService.AddToCartAsync(
                CurrentUserId, GetOrCreateSessionId(), productId, quantity);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, cartCount = count });

            return RedirectToAction("Index");
        }

        // POST: /Cart/Update
        [HttpPost]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            await _cartService.UpdateQuantityAsync(cartItemId, quantity);
            var vm = await _cartService.GetCartAsync(CurrentUserId, GetOrCreateSessionId());

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new
                {
                    success = true,
                    totalAmount = vm.TotalAmount,
                    totalQuantity = vm.TotalQuantity,
                    itemSubTotal = vm.Items.FirstOrDefault(i => i.CartItemID == cartItemId)?.SubTotal ?? 0
                });

            return RedirectToAction("Index");
        }

        // POST: /Cart/Remove
        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            await _cartService.RemoveItemAsync(cartItemId);
            var vm = await _cartService.GetCartAsync(CurrentUserId, GetOrCreateSessionId());

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new
                {
                    success = true,
                    totalAmount = vm.TotalAmount,
                    totalQuantity = vm.TotalQuantity
                });

            return RedirectToAction("Index");
        }

        // GET: /Cart/Count
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var count = await _cartService.GetCartCountAsync(CurrentUserId, GetOrCreateSessionId());
            return Json(new { count });
        }
    }
}