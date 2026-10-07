using Roselle.Web.Models.ViewModels;

namespace Roselle.Web.Services
{
    public interface ICartService
    {
        Task<int> AddToCartAsync(int? userId, string? sessionId, int productId, int quantity);
        Task<CartViewModel> GetCartAsync(int? userId, string? sessionId);
        Task<int> UpdateQuantityAsync(int cartItemId, int quantity);
        Task<bool> RemoveItemAsync(int cartItemId);
        Task ClearCartAsync(int? userId, string? sessionId);
        Task<int> GetCartCountAsync(int? userId, string? sessionId);
        Task MergeSessionCartToUserAsync(string sessionId, int userId);
    }
}