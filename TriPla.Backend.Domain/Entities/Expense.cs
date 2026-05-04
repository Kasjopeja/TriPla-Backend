using TriPla.Backend.Domain.ValueObjects;

namespace TriPla.Backend.Domain.Entities;

public class Expense
{
    public Guid Id { get; private set; }
    public Guid TripId { get; private set; }
    public Guid PaidByUserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public Money Amount { get; private set; } = null!;
    public ExpenseCategory Category { get; private set; }
    public DateTime Date { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsSettled { get; private set; }

    private readonly List<ExpenseSplit> _splits = new();
    public IReadOnlyCollection<ExpenseSplit> Splits => _splits.AsReadOnly();

    private Expense() { }

    public Expense(Guid tripId, Guid paidByUserId, string title, string? description,
        Money amount, ExpenseCategory category, DateTime date)
    {
        if (tripId == Guid.Empty) throw new ArgumentException("Trip ID cannot be empty.", nameof(tripId));
        if (paidByUserId == Guid.Empty) throw new ArgumentException("Payer user ID cannot be empty.", nameof(paidByUserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Amount <= 0)
            throw new ArgumentException("Expense amount must be positive.", nameof(amount));

        Id = Guid.NewGuid();
        TripId = tripId;
        PaidByUserId = paidByUserId;
        Title = title;
        Description = description;
        Amount = amount;
        Category = category;
        Date = date;
        CreatedAt = DateTime.UtcNow;
    }

    public static Expense Rehydrate(Guid id, Guid tripId, Guid paidByUserId, string title,
        string? description, Money amount, ExpenseCategory category, DateTime date, DateTime createdAt,
        bool isSettled = false)
    {
        return new Expense
        {
            Id = id,
            TripId = tripId,
            PaidByUserId = paidByUserId,
            Title = title,
            Description = description,
            Amount = amount,
            Category = category,
            Date = date,
            CreatedAt = createdAt,
            IsSettled = isSettled
        };
    }

    public void SetSettled(bool value) => IsSettled = value;

    public void AddSplit(ExpenseSplit split)
    {
        ArgumentNullException.ThrowIfNull(split);
        if (split.Amount.Currency != Amount.Currency)
            throw new InvalidOperationException("Split currency must match expense currency.");
        _splits.Add(split);
    }

    public void ClearSplits() => _splits.Clear();

    public void Update(string title, string? description, Money amount,
        ExpenseCategory category, DateTime date)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Amount <= 0)
            throw new ArgumentException("Expense amount must be positive.", nameof(amount));

        Title = title;
        Description = description;
        Amount = amount;
        Category = category;
        Date = date;
    }

    public void ValidateSplitsSum()
    {
        if (_splits.Count == 0) return;

        var total = _splits.Aggregate(0m, (acc, s) => acc + s.Amount.Amount);
        if (total != Amount.Amount)
            throw new InvalidOperationException(
                $"Sum of splits ({total}) must equal expense amount ({Amount.Amount}).");
    }
}
