using Microsoft.EntityFrameworkCore;
using Roselle.Web.Data;
using Roselle.Web.Models;
using Roselle.Web.Models.ViewModels;

namespace Roselle.Web.Services
{
    public class CartService : ICartService
    {
        private readonly RoselleDbContext _db;

        public CartService(RoselleDbContext db)
        {
            _db = db;
        }

        private async Task<Cart> GetOrCreateCartAsync(int? userId, string? sessionId)
        {
            Cart? cart = null;

            if (userId.HasValue)
            {
                cart = await _db.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserID == userId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                cart = await _db.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.SessionID == sessionId);
            }

            if (cart == null)
            {
                cart = new Cart
                {
                    UserID = userId,
                    SessionID = userId.HasValue ? null : sessionId,
                    CreatedAt = DateTime.Now
                };
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync();
            }

            return cart;
        }

        public async Task<int> AddToCartAsync(int? userId, string? sessionId, int productId, int quantity)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);

            var product = await _db.Products.FindAsync(productId);
            if (product == null) return 0;

            var existing = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.CartID == cart.CartID && ci.ProductID == productId);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    CartID = cart.CartID,
                    ProductID = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price
                });
            }

            cart.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return await GetCartCountAsync(userId, sessionId);
        }

        public async Task<CartViewModel> GetCartAsync(int? userId, string? sessionId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);

            var items = await _db.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CartID == cart.CartID)
                .ToListAsync();

            return new CartViewModel { Items = items };
        }

        public async Task<int> UpdateQuantityAsync(int cartItemId, int quantity)
        {
            if (quantity <= 0) return 0;

            var item = await _db.CartItems.FindAsync(cartItemId);
            if (item == null) return 0;

            item.Quantity = quantity;
            await _db.SaveChangesAsync();
            return quantity;
        }

        public async Task<bool> RemoveItemAsync(int cartItemId)
        {
            var item = await _db.CartItems.FindAsync(cartItemId);
            if (item == null) return false;

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task ClearCartAsync(int? userId, string? sessionId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            var items = _db.CartItems.Where(ci => ci.CartID == cart.CartID);
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
        }

        public async Task<int> GetCartCountAsync(int? userId, string? sessionId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            return await _db.CartItems
                .Where(ci => ci.CartID == cart.CartID)
                .SumAsync(ci => ci.Quantity);
        }

        public async Task MergeSessionCartToUserAsync(string sessionId, int userId)
        {
            var sessionCart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.SessionID == sessionId);

            if (sessionCart == null || !sessionCart.Items.Any()) return;

            var userCart = await GetOrCreateCartAsync(userId, null);

            foreach (var item in sessionCart.Items)
            {
                var existing = await _db.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartID == userCart.CartID && ci.ProductID == item.ProductID);

                if (existing != null)
                {
                    existing.Quantity += item.Quantity;
                }
                else
                {
                    _db.CartItems.Add(new CartItem
                    {
                        CartID = userCart.CartID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }
            }

            // Xóa session cart
            _db.Carts.Remove(sessionCart);
            await _db.SaveChangesAsync();
        }
    }
}