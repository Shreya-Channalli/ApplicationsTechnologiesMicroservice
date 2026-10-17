
using Microsoft.Data.SqlClient;

namespace ApplicationsToolsTechnologies.Data;

public sealed class DatabaseConnectionTest
{
    private readonly SqlConnection _connection;

    public DatabaseConnectionTest(SqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<string> TestAsync()
    {
        await _connection.OpenAsync();

        await using var command = new SqlCommand(
            "SELECT DB_NAME();", _connection);

        var result = await command.ExecuteScalarAsync();

        return result?.ToString() ?? "Connected, but database name was not returned.";
    }
}
