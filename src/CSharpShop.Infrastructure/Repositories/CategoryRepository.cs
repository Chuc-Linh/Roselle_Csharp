using CSharpShop.Application.Interfaces;
using CSharpShop.Application.Models;
using Microsoft.Data.SqlClient;

namespace CSharpShop.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly string _connectionString;

    public CategoryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        var categories = new List<Category>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = """
            SELECT Id, Name, Description, IsActive
            FROM dbo.Categories
            ORDER BY Id
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            categories.Add(new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                IsActive = reader.GetBoolean(3)
            });
        }

        return categories;
    }
}