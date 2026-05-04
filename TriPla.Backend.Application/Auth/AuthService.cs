using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.Auth;
using TriPla.Backend.Application.Interfaces;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;

namespace TriPla.Backend.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;

    public AuthService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ITokenProvider tokenProvider)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenProvider = tokenProvider;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await _unitOfWork.Users.ExistsByEmailAsync(request.Email, ct))
            return Result.Failure<AuthResponse>("User with this email already exists.");

        var hash = _passwordHasher.Hash(request.Password);
        var user = new User(request.FirstName, request.LastName, request.Email, hash);

        await _unitOfWork.Users.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var token = _tokenProvider.GenerateToken(user);
        return Result.Success(new AuthResponse(user.Id, user.Email, token));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, ct);
        if (user is null)
            return Result.Failure<AuthResponse>("Invalid email or password.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<AuthResponse>("Invalid email or password.");

        var token = _tokenProvider.GenerateToken(user);
        return Result.Success(new AuthResponse(user.Id, user.Email, token));
    }
}

