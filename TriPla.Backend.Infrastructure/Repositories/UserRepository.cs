using Npgsql;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;
using TriPla.Backend.Infrastructure.Persistence;

namespace TriPla.Backend.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly INpgsqlConnectionFactory _factory;

    public UserRepository(INpgsqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, first_name, last_name, email, password_hash, created_at
            FROM users
            WHERE id = @id
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return Map(reader);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, first_name, last_name, email, password_hash, created_at
            FROM users
            WHERE email = @email
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email.ToLowerInvariant());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return Map(reader);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM users WHERE email = @email)";

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email.ToLowerInvariant());

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool b && b;
    }

    public async Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idArray = ids.Distinct().ToArray();
        if (idArray.Length == 0) return Array.Empty<User>();

        const string sql = """
            SELECT id, first_name, last_name, email, password_hash, created_at
            FROM users
            WHERE id = ANY(@ids)
        """;

        var users = new List<User>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("ids", idArray);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            users.Add(Map(reader));
        return users;
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, first_name, last_name, email, password_hash, created_at
            FROM users
            ORDER BY created_at DESC
        """;

        var users = new List<User>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            users.Add(Map(reader));

        return users;
    }

    public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO users (id, first_name, last_name, email, password_hash, created_at)
            VALUES (@id, @firstName, @lastName, @email, @passwordHash, @createdAt)
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("firstName", entity.FirstName);
        command.Parameters.AddWithValue("lastName", entity.LastName);
        command.Parameters.AddWithValue("email", entity.Email);
        command.Parameters.AddWithValue("passwordHash", entity.PasswordHash);
        command.Parameters.AddWithValue("createdAt", entity.CreatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(User entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE users
            SET first_name = @firstName, last_name = @lastName
            WHERE id = @id
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("firstName", entity.FirstName);
        command.Parameters.AddWithValue("lastName", entity.LastName);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM users WHERE id = @id";

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static User Map(NpgsqlDataReader reader) => User.Rehydrate(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        reader.GetDateTime(5));
}
