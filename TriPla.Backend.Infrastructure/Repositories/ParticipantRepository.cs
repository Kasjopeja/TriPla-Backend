using Npgsql;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;
using TriPla.Backend.Infrastructure.Persistence;

namespace TriPla.Backend.Infrastructure.Repositories;

public class ParticipantRepository : IParticipantRepository
{
    private readonly INpgsqlConnectionFactory _factory;

    public ParticipantRepository(INpgsqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Participant?> GetAsync(Guid tripId, Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, trip_id, user_id, role, joined_at
            FROM participants WHERE trip_id = @tripId AND user_id = @userId
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tripId", tripId);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return Map(reader);
    }

    public async Task<IEnumerable<Participant>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, trip_id, user_id, role, joined_at
            FROM participants WHERE trip_id = @tripId
            ORDER BY joined_at
        """;

        var items = new List<Participant>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tripId", tripId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(Map(reader));
        return items;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsByTripIdsAsync(IEnumerable<Guid> tripIds, CancellationToken cancellationToken = default)
    {
        var idArray = tripIds.Distinct().ToArray();
        if (idArray.Length == 0) return new Dictionary<Guid, int>();

        const string sql = """
            SELECT trip_id, COUNT(*)::int
            FROM participants
            WHERE trip_id = ANY(@tripIds)
            GROUP BY trip_id
        """;

        var result = new Dictionary<Guid, int>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tripIds", idArray);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result[reader.GetGuid(0)] = reader.GetInt32(1);
        return result;
    }

    public async Task AddAsync(Participant participant, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO participants (id, trip_id, user_id, role, joined_at)
            VALUES (@id, @tripId, @userId, @role, @joinedAt)
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", participant.Id);
        command.Parameters.AddWithValue("tripId", participant.TripId);
        command.Parameters.AddWithValue("userId", participant.UserId);
        command.Parameters.AddWithValue("role", (int)participant.Role);
        command.Parameters.AddWithValue("joinedAt", participant.JoinedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Participant participant, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE participants SET role = @role
            WHERE trip_id = @tripId AND user_id = @userId
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tripId", participant.TripId);
        command.Parameters.AddWithValue("userId", participant.UserId);
        command.Parameters.AddWithValue("role", (int)participant.Role);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid tripId, Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM participants WHERE trip_id = @tripId AND user_id = @userId";

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tripId", tripId);
        command.Parameters.AddWithValue("userId", userId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Participant Map(NpgsqlDataReader reader) => Participant.Rehydrate(
        reader.GetGuid(0),
        reader.GetGuid(1),
        reader.GetGuid(2),
        (ParticipantRole)reader.GetInt32(3),
        reader.GetDateTime(4));
}
