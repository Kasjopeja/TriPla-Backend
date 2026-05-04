namespace TriPla.Backend.Domain.Entities;

public class Participant
{
    public Guid Id { get; private set; }
    public Guid TripId { get; private set; }
    public Guid UserId { get; private set; }
    public ParticipantRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private Participant() { }

    public Participant(Guid tripId, Guid userId, ParticipantRole role = ParticipantRole.Member)
    {
        if (tripId == Guid.Empty) throw new ArgumentException("Trip ID cannot be empty.", nameof(tripId));
        if (userId == Guid.Empty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        Id = Guid.NewGuid();
        TripId = tripId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
    }

    public static Participant Rehydrate(Guid id, Guid tripId, Guid userId, ParticipantRole role, DateTime joinedAt)
    {
        return new Participant
        {
            Id = id,
            TripId = tripId,
            UserId = userId,
            Role = role,
            JoinedAt = joinedAt
        };
    }

    public void ChangeRole(ParticipantRole newRole)
    {
        Role = newRole;
    }
}
