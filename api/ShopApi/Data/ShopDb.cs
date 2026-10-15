using Microsoft.Data.SqlClient;

namespace ShopApi.Data;

public sealed class ShopDb(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("ShopDb")
        ?? Environment.GetEnvironmentVariable("SHOP_DB")
        ?? "Server=localhost;Database=QuanLyBanHoa;Integrated Security=True;TrustServerCertificate=True;Encrypt=True";

    public async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
