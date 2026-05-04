namespace TriPla.Backend.Domain.Entities;

public record TripChangeLogEntry(
    Guid TripId,
    string Type,
    Guid ActorId,
    string? ActorEmail,
    string? PayloadJson,
    DateTime OccurredAt);
