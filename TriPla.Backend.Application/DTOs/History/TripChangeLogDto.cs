namespace TriPla.Backend.Application.DTOs.History;

public record TripChangeLogDto(
    Guid TripId,
    string Type,
    Guid ActorId,
    string? ActorEmail,
    string? PayloadJson,
    DateTime OccurredAt);
