using AutoMapper;
using CSharpShop.Application.DTOs;
using CSharpShop.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CSharpShop.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _service;
    private readonly IMapper _mapper;

    public CategoriesController(CategoryService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetAll()
    {
        var categories = await _service.GetAllAsync();

        var result = _mapper.Map<List<CategoryDto>>(categories);

        return Ok(new ApiResponse<List<CategoryDto>>
        {
            Success = true,
            Message = "Lấy danh sách loại sản phẩm thành công.",
            Data = result
        });
    }
}