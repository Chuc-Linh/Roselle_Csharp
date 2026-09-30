using CSharpShop.Application.Models;

namespace CSharpShop.Application.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync();
}