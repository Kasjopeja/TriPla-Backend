namespace TriPla.Backend.Application.DTOs.Trips;

public record TripDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    Guid OwnerId,
    int ParticipantCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
