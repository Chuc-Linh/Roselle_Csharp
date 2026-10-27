using Microsoft.EntityFrameworkCore;
using Roselle.Web.Data;
using Roselle.Web.Models;
using Roselle.Web.Models.ViewModels;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Roselle.Web.Services
{
    public class CartService : ICartService
    {
        private readonly RoselleDbContext _db;
        private const string CartSessionKey = "GuestCart";

        public CartService(RoselleDbContext db)
        {
            _db = db;
        }

        // ==========================================
        // 1. CÁC HÀM XỬ LÝ GIỎ HÀNG DATABASE (ĐÃ CÓ)
        // ==========================================
        public async Task<int> AddToCartAsync(int customerId, int productId, int quantity)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null) return 0;

            var existing = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.CustomerID == customerId && ci.ProductID == productId);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    CustomerID = customerId,
                    ProductID = productId,
                    Quantity = quantity
                });
            }

            await _db.SaveChangesAsync();
            return await GetCartCountAsync(customerId);
        }

        public async Task<CartViewModel> GetCartAsync(int customerId)
        {
            var items = await _db.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CustomerID == customerId)
                .ToListAsync();

            return new CartViewModel { Items = items };
        }

        public async Task<int> UpdateQuantityAsync(int customerId, int productId, int quantity)
        {
            if (quantity <= 0)
            {
                await RemoveItemAsync(customerId, productId);
                return 0;
            }

            var item = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.CustomerID == customerId && ci.ProductID == productId);
            if (item == null) return 0;

            item.Quantity = quantity;
            await _db.SaveChangesAsync();
            return quantity;
        }

        public async Task<bool> RemoveItemAsync(int customerId, int productId)
        {
            var item = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.CustomerID == customerId && ci.ProductID == productId);
            if (item == null) return false;

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task ClearCartAsync(int customerId)
        {
            var items = _db.CartItems.Where(ci => ci.CustomerID == customerId);
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
        }

        public async Task<int> GetCartCountAsync(int customerId)
        {
            return await _db.CartItems
                .Where(ci => ci.CustomerID == customerId)
                .SumAsync(ci => ci.Quantity);
        }

        // ==========================================
        // 2. CÁC HÀM XỬ LÝ GIỎ HÀNG TẠM BẰNG SESSION
        // ==========================================
        public List<CartItem> GetGuestCart(ISession session)
        {
            var json = session.GetString(CartSessionKey);
            return string.IsNullOrEmpty(json) ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(json);
        }

        public void SaveGuestCart(ISession session, List<CartItem> cart)
        {
            session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }

        public void AddToGuestCart(ISession session, int productId, int quantity)
        {
            var cart = GetGuestCart(session);
            var existing = cart.FirstOrDefault(c => c.ProductID == productId);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem { ProductID = productId, Quantity = quantity });
            }
            SaveGuestCart(session, cart);
        }

        public void UpdateGuestCart(ISession session, int productId, int quantity)
        {
            var cart = GetGuestCart(session);
            var item = cart.FirstOrDefault(c => c.ProductID == productId);
            if (item != null)
            {
                if (quantity <= 0) cart.Remove(item);
                else item.Quantity = quantity;
                SaveGuestCart(session, cart);
            }
        }

        public void RemoveFromGuestCart(ISession session, int productId)
        {
            var cart = GetGuestCart(session);
            var item = cart.FirstOrDefault(c => c.ProductID == productId);
            if (item != null)
            {
                cart.Remove(item);
                SaveGuestCart(session, cart);
            }
        }

        public async Task MergeGuestCartToUserAsync(ISession session, int customerId)
        {
            var guestCart = GetGuestCart(session);
            if (guestCart.Count == 0) return;

            foreach (var item in guestCart)
            {
                var existing = await _db.CartItems
                    .FirstOrDefaultAsync(c => c.CustomerID == customerId && c.ProductID == item.ProductID);
                
                if (existing != null)
                {
                    existing.Quantity += item.Quantity;
                }
                else
                {
                    _db.CartItems.Add(new CartItem 
                    { 
                        CustomerID = customerId, 
                        ProductID = item.ProductID, 
                        Quantity = item.Quantity 
                    });
                }
            }
            await _db.SaveChangesAsync();
            session.Remove(CartSessionKey); // Xóa giỏ tạm sau khi gộp
        }
    }
}