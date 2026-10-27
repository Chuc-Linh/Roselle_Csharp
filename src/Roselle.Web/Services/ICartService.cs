using Roselle.Web.Models;
using Roselle.Web.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Roselle.Web.Services
{
    public interface ICartService
    {
        // Các hàm xử lý giỏ hàng cho người đã đăng nhập (Database)
        Task<int> AddToCartAsync(int customerId, int productId, int quantity);
        Task<CartViewModel> GetCartAsync(int customerId);
        Task<int> UpdateQuantityAsync(int customerId, int productId, int quantity);
        Task<bool> RemoveItemAsync(int customerId, int productId);
        Task ClearCartAsync(int customerId);
        Task<int> GetCartCountAsync(int customerId);

        // Các hàm xử lý giỏ hàng tạm cho khách vãng lai (Session)
        List<CartItem> GetGuestCart(ISession session);
        void SaveGuestCart(ISession session, List<CartItem> cart);
        void AddToGuestCart(ISession session, int productId, int quantity);
        void UpdateGuestCart(ISession session, int productId, int quantity);
        void RemoveFromGuestCart(ISession session, int productId);
        
        // Hàm gộp giỏ hàng từ Session vào Database khi đăng nhập thành công
        Task MergeGuestCartToUserAsync(ISession session, int customerId);
    }
}