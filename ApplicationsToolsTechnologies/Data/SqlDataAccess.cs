
using Microsoft.Data.SqlClient;

namespace ApplicationsToolsTechnologies.Data;

public sealed class SqlDataAccess
{
    private readonly string _connectionString;

    public SqlDataAccess(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("ConsilioDb")
            ?? throw new InvalidOperationException(
                "ConsilioDb connection string is missing.");
    }

    // Keeps existing parameter-free calls working.
    public Task<List<Dictionary<string, object?>>> QueryAsync(
        string sql,
        CancellationToken cancellationToken = default)
    {
        return QueryAsync(
            sql,
            Array.Empty<SqlParameter>(),
            cancellationToken);
    }

    // Use this overload for parameterized queries.
    public async Task<List<Dictionary<string, object?>>> QueryAsync(
        string sql,
        IEnumerable<SqlParameter> parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentNullException.ThrowIfNull(parameters);

        var results = new List<Dictionary<string, object?>>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(
                StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] =
                    await reader.IsDBNullAsync(i, cancellationToken)
                        ? null
                        : reader.GetValue(i);
            }

            results.Add(row);
        }

        return results;
    }
}
