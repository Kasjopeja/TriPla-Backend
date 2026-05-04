using Npgsql;

namespace TriPla.Backend.Infrastructure.Persistence;

public interface INpgsqlConnectionFactory
{
    Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}

public class NpgsqlConnectionFactory : INpgsqlConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        _connectionString = connectionString;
    }

    public async Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
