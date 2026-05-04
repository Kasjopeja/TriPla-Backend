using TriPla.Backend.Domain.ValueObjects;

namespace TriPla.Backend.Domain.Entities;

public class Trip
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateRange DateRange { get; private set; } = null!;
    public Guid OwnerId { get; private set; }

    private readonly List<Participant> _participants = new();
    private readonly List<Attraction> _attractions = new();
    private readonly List<Expense> _expenses = new();
    private readonly List<Comment> _comments = new();

    public IReadOnlyCollection<Participant> Participants => _participants.AsReadOnly();
    public IReadOnlyCollection<Attraction> Attractions => _attractions.AsReadOnly();
    public IReadOnlyCollection<Expense> Expenses => _expenses.AsReadOnly();
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Trip() { }

    public Trip(string name, string? description, DateRange dateRange, Guid ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(dateRange);
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));

        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        DateRange = dateRange;
        OwnerId = ownerId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Trip Rehydrate(Guid id, string name, string? description, DateRange dateRange,
        Guid ownerId, DateTime createdAt, DateTime updatedAt)
    {
        return new Trip
        {
            Id = id,
            Name = name,
            Description = description,
            DateRange = dateRange,
            OwnerId = ownerId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void Update(string name, string? description, DateRange dateRange)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(dateRange);

        Name = name;
        Description = description;
        DateRange = dateRange;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddParticipant(Participant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        if (_participants.Any(p => p.UserId == participant.UserId))
            throw new InvalidOperationException("User is already a participant of this trip.");

        _participants.Add(participant);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveParticipant(Guid userId)
    {
        if (userId == OwnerId)
            throw new InvalidOperationException("Owner cannot be removed from the trip.");

        var participant = _participants.FirstOrDefault(p => p.UserId == userId)
            ?? throw new InvalidOperationException("Participant not found.");

        _participants.Remove(participant);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeParticipantRole(Guid userId, ParticipantRole newRole)
    {
        if (userId == OwnerId)
            throw new InvalidOperationException("Cannot change the role of the trip owner.");

        var participant = _participants.FirstOrDefault(p => p.UserId == userId)
            ?? throw new InvalidOperationException("Participant not found.");

        participant.ChangeRole(newRole);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasParticipant(Guid userId) =>
        _participants.Any(p => p.UserId == userId);

    public ParticipantRole? GetRole(Guid userId) =>
        _participants.FirstOrDefault(p => p.UserId == userId)?.Role;

    public void AddAttraction(Attraction attraction)
    {
        ArgumentNullException.ThrowIfNull(attraction);
        _attractions.Add(attraction);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveAttraction(Guid attractionId)
    {
        var attraction = _attractions.FirstOrDefault(a => a.Id == attractionId)
            ?? throw new InvalidOperationException("Attraction not found.");

        _attractions.Remove(attraction);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddExpense(Expense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);
        _expenses.Add(expense);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveExpense(Guid expenseId)
    {
        var expense = _expenses.FirstOrDefault(e => e.Id == expenseId)
            ?? throw new InvalidOperationException("Expense not found.");

        _expenses.Remove(expense);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddComment(Comment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);
        _comments.Add(comment);
        UpdatedAt = DateTime.UtcNow;
    }
}
