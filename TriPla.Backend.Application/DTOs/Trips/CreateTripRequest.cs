namespace TriPla.Backend.Application.DTOs.Trips;

public record CreateTripRequest(
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate);

