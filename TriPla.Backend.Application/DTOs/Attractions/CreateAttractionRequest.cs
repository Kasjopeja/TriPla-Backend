namespace TriPla.Backend.Application.DTOs.Attractions;

public record CreateAttractionRequest(
    string Name,
    string? Description,
    string? Street,
    string? City,
    string? Country,
    DateTime? PlannedAt);

