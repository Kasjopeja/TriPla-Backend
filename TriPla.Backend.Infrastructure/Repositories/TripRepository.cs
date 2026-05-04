using Npgsql;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;
using TriPla.Backend.Domain.ValueObjects;
using TriPla.Backend.Infrastructure.Persistence;

namespace TriPla.Backend.Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly INpgsqlConnectionFactory _factory;

    public TripRepository(INpgsqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, name, description, start_date, end_date, owner_id, created_at, updated_at
            FROM trips
            WHERE id = @id
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return Map(reader);
    }

    public async Task<Trip?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var trip = await GetByIdAsync(id, cancellationToken);
        if (trip is null) return null;

        await LoadParticipants(trip, cancellationToken);
        await LoadAttractions(trip, cancellationToken);
        await LoadExpenses(trip, cancellationToken);
        await LoadComments(trip, cancellationToken);

        return trip;
    }

    public async Task<IEnumerable<Trip>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, name, description, start_date, end_date, owner_id, created_at, updated_at
            FROM trips
            ORDER BY created_at DESC
        """;

        var trips = new List<Trip>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            trips.Add(Map(reader));
        return trips;
    }

    public async Task<IEnumerable<Trip>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, name, description, start_date, end_date, owner_id, created_at, updated_at
            FROM trips
            WHERE owner_id = @ownerId
            ORDER BY start_date DESC
        """;

        var trips = new List<Trip>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("ownerId", ownerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            trips.Add(Map(reader));
        return trips;
    }

    public async Task<IEnumerable<Trip>> GetByParticipantIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT t.id, t.name, t.description, t.start_date, t.end_date, t.owner_id, t.created_at, t.updated_at
            FROM trips t
            INNER JOIN participants p ON p.trip_id = t.id
            WHERE p.user_id = @userId
            ORDER BY t.start_date DESC
        """;

        var trips = new List<Trip>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            trips.Add(Map(reader));
        return trips;
    }

    public async Task AddAsync(Trip entity, CancellationToken cancellationToken = default)
    {
        const string insertTrip = """
            INSERT INTO trips (id, name, description, start_date, end_date, owner_id, created_at, updated_at)
            VALUES (@id, @name, @description, @startDate, @endDate, @ownerId, @createdAt, @updatedAt)
        """;
        const string insertParticipant = """
            INSERT INTO participants (id, trip_id, user_id, role, joined_at)
            VALUES (@id, @tripId, @userId, @role, @joinedAt)
            ON CONFLICT (trip_id, user_id) DO NOTHING
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var command = new NpgsqlCommand(insertTrip, connection, transaction))
        {
            command.Parameters.AddWithValue("id", entity.Id);
            command.Parameters.AddWithValue("name", entity.Name);
            command.Parameters.AddWithValue("description", (object?)entity.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("startDate", entity.DateRange.StartDate);
            command.Parameters.AddWithValue("endDate", entity.DateRange.EndDate);
            command.Parameters.AddWithValue("ownerId", entity.OwnerId);
            command.Parameters.AddWithValue("createdAt", entity.CreatedAt);
            command.Parameters.AddWithValue("updatedAt", entity.UpdatedAt);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var participant in entity.Participants)
        {
            await using var cmd = new NpgsqlCommand(insertParticipant, connection, transaction);
            cmd.Parameters.AddWithValue("id", participant.Id);
            cmd.Parameters.AddWithValue("tripId", participant.TripId);
            cmd.Parameters.AddWithValue("userId", participant.UserId);
            cmd.Parameters.AddWithValue("role", (int)participant.Role);
            cmd.Parameters.AddWithValue("joinedAt", participant.JoinedAt);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(Trip entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE trips
            SET name = @name, description = @description, start_date = @startDate,
                end_date = @endDate, updated_at = @updatedAt
            WHERE id = @id
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("name", entity.Name);
        command.Parameters.AddWithValue("description", (object?)entity.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("startDate", entity.DateRange.StartDate);
        command.Parameters.AddWithValue("endDate", entity.DateRange.EndDate);
        command.Parameters.AddWithValue("updatedAt", entity.UpdatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM trips WHERE id = @id";

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task LoadParticipants(Trip trip, CancellationToken ct)
    {
        const string sql = """
            SELECT id, trip_id, user_id, role, joined_at
            FROM participants WHERE trip_id = @tripId
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tripId", trip.Id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var participant = Participant.Rehydrate(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                (ParticipantRole)reader.GetInt32(3),
                reader.GetDateTime(4));
            trip.AddParticipant(participant);
        }
    }

    private async Task LoadAttractions(Trip trip, CancellationToken ct)
    {
        const string sql = """
            SELECT id, trip_id, name, description, street, city, country, postal_code, planned_at, created_at
            FROM attractions WHERE trip_id = @tripId
            ORDER BY planned_at NULLS LAST, created_at
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tripId", trip.Id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
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

            var attraction = Attraction.Rehydrate(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                address,
                reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                reader.GetDateTime(9));
            trip.AddAttraction(attraction);
        }
    }

    private async Task LoadExpenses(Trip trip, CancellationToken ct)
    {
        const string sql = """
            SELECT id, trip_id, paid_by_user_id, title, description, amount, currency, category, date, created_at, is_settled
            FROM expenses WHERE trip_id = @tripId
            ORDER BY date DESC
        """;

        var loadedExpenses = new List<Expense>();
        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using (var command = new NpgsqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("tripId", trip.Id);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var money = new Money(reader.GetDecimal(5), reader.GetString(6));
                var expense = Expense.Rehydrate(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetGuid(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    money,
                    (ExpenseCategory)reader.GetInt32(7),
                    reader.GetDateTime(8),
                    reader.GetDateTime(9),
                    reader.GetBoolean(10));
                loadedExpenses.Add(expense);
                trip.AddExpense(expense);
            }
        }

        if (loadedExpenses.Count == 0) return;

        const string splitsSql = """
            SELECT id, expense_id, user_id, amount, currency
            FROM expense_splits
            WHERE expense_id = ANY(@expenseIds)
        """;

        var expenseIds = loadedExpenses.Select(e => e.Id).ToArray();
        var byId = loadedExpenses.ToDictionary(e => e.Id);

        await using var splitsCmd = new NpgsqlCommand(splitsSql, connection);
        splitsCmd.Parameters.AddWithValue("expenseIds", expenseIds);
        await using var splitsReader = await splitsCmd.ExecuteReaderAsync(ct);
        while (await splitsReader.ReadAsync(ct))
        {
            var expenseId = splitsReader.GetGuid(1);
            if (!byId.TryGetValue(expenseId, out var expense)) continue;

            var money = new Money(splitsReader.GetDecimal(3), splitsReader.GetString(4));
            var split = ExpenseSplit.Rehydrate(
                splitsReader.GetGuid(0),
                expenseId,
                splitsReader.GetGuid(2),
                money);
            expense.AddSplit(split);
        }
    }

    private async Task LoadComments(Trip trip, CancellationToken ct)
    {
        const string sql = """
            SELECT id, trip_id, author_id, parent_id, content, created_at, edited_at
            FROM comments WHERE trip_id = @tripId
            ORDER BY created_at
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tripId", trip.Id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var comment = Comment.Rehydrate(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetString(4),
                reader.GetDateTime(5),
                reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                reader.IsDBNull(3) ? null : reader.GetGuid(3));
            trip.AddComment(comment);
        }
    }

    private static Trip Map(NpgsqlDataReader reader)
    {
        var dateRange = new DateRange(reader.GetDateTime(3), reader.GetDateTime(4));
        return Trip.Rehydrate(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            dateRange,
            reader.GetGuid(5),
            reader.GetDateTime(6),
            reader.GetDateTime(7));
    }
}
