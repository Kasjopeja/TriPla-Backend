using TriPla.Backend.Domain.ValueObjects;

namespace TriPla.Backend.Domain.Entities;

public class ExpenseSplit
{
    public Guid Id { get; private set; }
    public Guid ExpenseId { get; private set; }
    public Guid UserId { get; private set; }
    public Money Amount { get; private set; } = null!;

    private ExpenseSplit() { }

    public ExpenseSplit(Guid expenseId, Guid userId, Money amount)
    {
        if (expenseId == Guid.Empty) throw new ArgumentException("Expense ID cannot be empty.", nameof(expenseId));
        if (userId == Guid.Empty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Amount <= 0)
            throw new ArgumentException("Split amount must be positive.", nameof(amount));

        Id = Guid.NewGuid();
        ExpenseId = expenseId;
        UserId = userId;
        Amount = amount;
    }

    public static ExpenseSplit Rehydrate(Guid id, Guid expenseId, Guid userId, Money amount)
    {
        return new ExpenseSplit
        {
            Id = id,
            ExpenseId = expenseId,
            UserId = userId,
            Amount = amount
        };
    }
}
