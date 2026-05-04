namespace TriPla.Backend.Application.DTOs.Trips;

public record UpdateTripRequest(
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate);

