namespace TriPla.Backend.Application.DTOs.Auth;

public record LoginRequest(
    string Email,
    string Password);

