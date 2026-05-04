using Npgsql;

namespace TriPla.Backend.Infrastructure.Persistence;

public class DatabaseInitializer
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public DatabaseInitializer(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(DatabaseSchema.CreateSchemaSql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
