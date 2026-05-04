using TriPla.Backend.Domain.ValueObjects;

namespace TriPla.Backend.Domain.Entities;

public class Attraction
{
    public Guid Id { get; private set; }
    public Guid TripId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Address? Address { get; private set; }
    public DateTime? PlannedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Attraction() { }

    public Attraction(Guid tripId, string name, string? description, Address? address, DateTime? plannedAt)
    {
        if (tripId == Guid.Empty) throw new ArgumentException("Trip ID cannot be empty.", nameof(tripId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = Guid.NewGuid();
        TripId = tripId;
        Name = name;
        Description = description;
        Address = address;
        PlannedAt = plannedAt;
        CreatedAt = DateTime.UtcNow;
    }

    public static Attraction Rehydrate(Guid id, Guid tripId, string name, string? description,
        Address? address, DateTime? plannedAt, DateTime createdAt)
    {
        return new Attraction
        {
            Id = id,
            TripId = tripId,
            Name = name,
            Description = description,
            Address = address,
            PlannedAt = plannedAt,
            CreatedAt = createdAt
        };
    }

    public void Update(string name, string? description, Address? address, DateTime? plannedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Description = description;
        Address = address;
        PlannedAt = plannedAt;
    }
}
