using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.Auth;

namespace TriPla.Backend.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

