
using CSharpShop.Application.Interfaces;
using CSharpShop.Application.Services;
using CSharpShop.Infrastructure.Repositories;
using CSharpShop.Api.Middlewares;
using CSharpShop.Application.Mappings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<ICategoryRepository>(_ =>
{
    var connectionString = builder.Configuration.GetConnectionString("ShopDb")
        ?? throw new InvalidOperationException("Thiếu chuỗi kết nối ShopDb.");

    return new CategoryRepository(connectionString);
});

builder.Services.AddScoped<CategoryService>();
builder.Services.AddAutoMapper(
    configuration => configuration.AddProfile<CategoryProfile>());

var app = builder.Build();
app.UseMiddleware<GlobalExceptionMiddleware>();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/test-error", () =>
    {
        throw new InvalidOperationException("Lỗi thử nghiệm.");
    });
}
app.Run();
