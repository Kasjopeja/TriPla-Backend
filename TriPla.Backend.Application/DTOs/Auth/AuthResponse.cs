namespace TriPla.Backend.Application.DTOs.Auth;

public record AuthResponse(
    Guid UserId,
    string Email,
    string Token);

