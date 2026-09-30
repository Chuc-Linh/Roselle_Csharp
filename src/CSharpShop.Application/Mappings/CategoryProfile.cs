using AutoMapper;
using CSharpShop.Application.DTOs;
using CSharpShop.Application.Models;

namespace CSharpShop.Application.Mappings;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDto>();
    }
}