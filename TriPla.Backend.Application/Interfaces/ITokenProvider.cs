using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Application.Interfaces;

public interface ITokenProvider
{
    string GenerateToken(User user);
}

