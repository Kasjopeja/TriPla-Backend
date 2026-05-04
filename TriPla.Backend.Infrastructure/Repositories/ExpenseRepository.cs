using Npgsql;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;
using TriPla.Backend.Domain.ValueObjects;
using TriPla.Backend.Infrastructure.Persistence;

namespace TriPla.Backend.Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private const string SelectColumns =
        "id, trip_id, paid_by_user_id, title, description, amount, currency, category, date, created_at, is_settled";

    private readonly INpgsqlConnectionFactory _factory;

    public ExpenseRepository(INpgsqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {SelectColumns} FROM expenses WHERE id = @id";

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var expense = Map(reader);
        await reader.CloseAsync();
        await LoadSplits(expense, cancellationToken);
        return expense;
    }

    public async Task<IEnumerable<Expense>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {SelectColumns}
            FROM expenses WHERE trip_id = @tripId
            ORDER BY date DESC
        """;

        var items = new List<Expense>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using (var command = new NpgsqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("tripId", tripId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                items.Add(Map(reader));
        }

        foreach (var item in items)
            await LoadSplits(item, cancellationToken);

        return items;
    }

    public async Task<IEnumerable<Expense>> GetByPaidByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {SelectColumns}
            FROM expenses WHERE paid_by_user_id = @userId
            ORDER BY date DESC
        """;

        var items = new List<Expense>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(Map(reader));
        return items;
    }

    public async Task<IEnumerable<Expense>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {SelectColumns} FROM expenses";
        var items = new List<Expense>();
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(Map(reader));
        return items;
    }

    public async Task AddAsync(Expense entity, CancellationToken cancellationToken = default)
    {
        const string insertExpense = """
            INSERT INTO expenses (id, trip_id, paid_by_user_id, title, description, amount, currency, category, date, created_at, is_settled)
            VALUES (@id, @tripId, @paidByUserId, @title, @description, @amount, @currency, @category, @date, @createdAt, @isSettled)
        """;
        const string insertSplit = """
            INSERT INTO expense_splits (id, expense_id, user_id, amount, currency)
            VALUES (@id, @expenseId, @userId, @amount, @currency)
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var command = new NpgsqlCommand(insertExpense, connection, transaction))
        {
            command.Parameters.AddWithValue("id", entity.Id);
            command.Parameters.AddWithValue("tripId", entity.TripId);
            command.Parameters.AddWithValue("paidByUserId", entity.PaidByUserId);
            command.Parameters.AddWithValue("title", entity.Title);
            command.Parameters.AddWithValue("description", (object?)entity.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("amount", entity.Amount.Amount);
            command.Parameters.AddWithValue("currency", entity.Amount.Currency);
            command.Parameters.AddWithValue("category", (int)entity.Category);
            command.Parameters.AddWithValue("date", entity.Date);
            command.Parameters.AddWithValue("createdAt", entity.CreatedAt);
            command.Parameters.AddWithValue("isSettled", entity.IsSettled);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var split in entity.Splits)
        {
            await using var cmd = new NpgsqlCommand(insertSplit, connection, transaction);
            cmd.Parameters.AddWithValue("id", split.Id);
            cmd.Parameters.AddWithValue("expenseId", split.ExpenseId);
            cmd.Parameters.AddWithValue("userId", split.UserId);
            cmd.Parameters.AddWithValue("amount", split.Amount.Amount);
            cmd.Parameters.AddWithValue("currency", split.Amount.Currency);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(Expense entity, CancellationToken cancellationToken = default)
    {
        const string updateExpense = """
            UPDATE expenses
            SET title = @title, description = @description, amount = @amount,
                currency = @currency, category = @category, date = @date
            WHERE id = @id
        """;
        const string deleteSplits = "DELETE FROM expense_splits WHERE expense_id = @expenseId";
        const string insertSplit = """
            INSERT INTO expense_splits (id, expense_id, user_id, amount, currency)
            VALUES (@id, @expenseId, @userId, @amount, @currency)
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var command = new NpgsqlCommand(updateExpense, connection, transaction))
        {
            command.Parameters.AddWithValue("id", entity.Id);
            command.Parameters.AddWithValue("title", entity.Title);
            command.Parameters.AddWithValue("description", (object?)entity.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("amount", entity.Amount.Amount);
            command.Parameters.AddWithValue("currency", entity.Amount.Currency);
            command.Parameters.AddWithValue("category", (int)entity.Category);
            command.Parameters.AddWithValue("date", entity.Date);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteCmd = new NpgsqlCommand(deleteSplits, connection, transaction))
        {
            deleteCmd.Parameters.AddWithValue("expenseId", entity.Id);
            await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var split in entity.Splits)
        {
            await using var cmd = new NpgsqlCommand(insertSplit, connection, transaction);
            cmd.Parameters.AddWithValue("id", split.Id);
            cmd.Parameters.AddWithValue("expenseId", split.ExpenseId);
            cmd.Parameters.AddWithValue("userId", split.UserId);
            cmd.Parameters.AddWithValue("amount", split.Amount.Amount);
            cmd.Parameters.AddWithValue("currency", split.Amount.Currency);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM expenses WHERE id = @id";
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetSettledAsync(Guid expenseId, bool isSettled, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE expenses SET is_settled = @isSettled WHERE id = @id";
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", expenseId);
        command.Parameters.AddWithValue("isSettled", isSettled);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> SetAllSettledAsync(Guid tripId, bool isSettled, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE expenses SET is_settled = @isSettled
            WHERE trip_id = @tripId AND is_settled <> @isSettled
        """;
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tripId", tripId);
        command.Parameters.AddWithValue("isSettled", isSettled);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task LoadSplits(Expense expense, CancellationToken ct)
    {
        const string sql = """
            SELECT id, expense_id, user_id, amount, currency
            FROM expense_splits WHERE expense_id = @expenseId
        """;

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("expenseId", expense.Id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var money = new Money(reader.GetDecimal(3), reader.GetString(4));
            var split = ExpenseSplit.Rehydrate(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                money);
            expense.AddSplit(split);
        }
    }

    private static Expense Map(NpgsqlDataReader reader)
    {
        var money = new Money(reader.GetDecimal(5), reader.GetString(6));
        return Expense.Rehydrate(
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
    }
}
