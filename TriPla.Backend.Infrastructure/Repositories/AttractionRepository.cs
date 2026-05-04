using Npgsql;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;
using TriPla.Backend.Domain.ValueObjects;
using TriPla.Backend.Infrastructure.Persistence;

namespace TriPla.Backend.Infrastructure.Repositories;

public class AttractionRepository : IAttractionRepository
{
    private readonly INpgsqlConnectionFactory _factory;

    public AttractionRepository(INpgsqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Attraction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, trip_id, name, description, street, city, country, postal_code, planned_at, created_at
            FROM attractions WHERE id = @id
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return Map(reader);
    }

    public async Task<IEnumerable<Attraction>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, trip_id, name, description, street, city, country, postal_code, planned_at, created_at
            FROM attractions WHERE trip_id = @tripId
            ORDER BY planned_at NULLS LAST, created_at
        """;

        var items = new List<Attraction>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tripId", tripId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(Map(reader));
        return items;
    }

    public async Task<IEnumerable<Attraction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, trip_id, name, description, street, city, country, postal_code, planned_at, created_at
            FROM attractions
        """;

        var items = new List<Attraction>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(Map(reader));
        return items;
    }

    public async Task AddAsync(Attraction entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO attractions (id, trip_id, name, description, street, city, country, postal_code, planned_at, created_at)
            VALUES (@id, @tripId, @name, @description, @street, @city, @country, @postalCode, @plannedAt, @createdAt)
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("tripId", entity.TripId);
        command.Parameters.AddWithValue("name", entity.Name);
        command.Parameters.AddWithValue("description", (object?)entity.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("street", (object?)entity.Address?.Street ?? DBNull.Value);
        command.Parameters.AddWithValue("city", (object?)entity.Address?.City ?? DBNull.Value);
        command.Parameters.AddWithValue("country", (object?)entity.Address?.Country ?? DBNull.Value);
        command.Parameters.AddWithValue("postalCode", (object?)entity.Address?.PostalCode ?? DBNull.Value);
        command.Parameters.AddWithValue("plannedAt", (object?)entity.PlannedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("createdAt", entity.CreatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Attraction entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE attractions
            SET name = @name, description = @description, street = @street, city = @city,
                country = @country, postal_code = @postalCode, planned_at = @plannedAt
            WHERE id = @id
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("name", entity.Name);
        command.Parameters.AddWithValue("description", (object?)entity.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("street", (object?)entity.Address?.Street ?? DBNull.Value);
        command.Parameters.AddWithValue("city", (object?)entity.Address?.City ?? DBNull.Value);
        command.Parameters.AddWithValue("country", (object?)entity.Address?.Country ?? DBNull.Value);
        command.Parameters.AddWithValue("postalCode", (object?)entity.Address?.PostalCode ?? DBNull.Value);
        command.Parameters.AddWithValue("plannedAt", (object?)entity.PlannedAt ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM attractions WHERE id = @id";
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Attraction Map(NpgsqlDataReader reader)
    {
        Address? address = null;
        if (!reader.IsDBNull(4) && !reader.IsDBNull(5) && !reader.IsDBNull(6))
        {
            address = new Address(
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7));
        }

        return Attraction.Rehydrate(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            address,
            reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            reader.GetDateTime(9));
    }
}
