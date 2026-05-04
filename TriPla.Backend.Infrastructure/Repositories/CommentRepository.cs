using Npgsql;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;
using TriPla.Backend.Infrastructure.Persistence;

namespace TriPla.Backend.Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private const string SelectColumns =
        "id, trip_id, author_id, parent_id, content, created_at, edited_at";

    private readonly INpgsqlConnectionFactory _factory;

    public CommentRepository(INpgsqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {SelectColumns} FROM comments WHERE id = @id";

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return Map(reader);
    }

    public async Task<IEnumerable<Comment>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {SelectColumns}
            FROM comments WHERE trip_id = @tripId
            ORDER BY created_at
        """;

        var items = new List<Comment>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tripId", tripId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(Map(reader));
        return items;
    }

    public async Task<IEnumerable<Comment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {SelectColumns} FROM comments";

        var items = new List<Comment>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(Map(reader));
        return items;
    }

    public async Task AddAsync(Comment entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO comments (id, trip_id, author_id, parent_id, content, created_at, edited_at)
            VALUES (@id, @tripId, @authorId, @parentId, @content, @createdAt, @editedAt)
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("tripId", entity.TripId);
        command.Parameters.AddWithValue("authorId", entity.AuthorId);
        command.Parameters.AddWithValue("parentId", (object?)entity.ParentId ?? DBNull.Value);
        command.Parameters.AddWithValue("content", entity.Content);
        command.Parameters.AddWithValue("createdAt", entity.CreatedAt);
        command.Parameters.AddWithValue("editedAt", (object?)entity.EditedAt ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Comment entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE comments SET content = @content, edited_at = @editedAt WHERE id = @id
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("content", entity.Content);
        command.Parameters.AddWithValue("editedAt", (object?)entity.EditedAt ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM comments WHERE id = @id";
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Comment Map(NpgsqlDataReader reader) => Comment.Rehydrate(
        reader.GetGuid(0),
        reader.GetGuid(1),
        reader.GetGuid(2),
        reader.GetString(4),
        reader.GetDateTime(5),
        reader.IsDBNull(6) ? null : reader.GetDateTime(6),
        reader.IsDBNull(3) ? null : reader.GetGuid(3));
}
