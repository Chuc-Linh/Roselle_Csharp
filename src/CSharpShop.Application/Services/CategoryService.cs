using CSharpShop.Application.Interfaces;
using CSharpShop.Application.Models;

namespace CSharpShop.Application.Services;

public class CategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Category>> GetAllAsync()
    {
        return _repository.GetAllAsync();
    }
}