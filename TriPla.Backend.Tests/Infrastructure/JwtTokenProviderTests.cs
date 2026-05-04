using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Infrastructure.Identity;

namespace TriPla.Backend.Tests.Infrastructure;

[TestFixture]
public class JwtTokenProviderTests
{
    private static JwtTokenProvider CreateProvider()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "TriPla",
            Audience = "TriPla",
            SecretKey = "super_long_secret_key_for_unit_tests_1234567890",
            ExpirationMinutes = 30
        });
        return new JwtTokenProvider(options);
    }

    [Test]
    public void GenerateToken_ProducesValidJwtWithExpectedClaims()
    {
        var provider = CreateProvider();
        var user = new User("Alice", "Smith", "alice@example.com", "hash");

        var token = provider.GenerateToken(user);
        token.Should().NotBeNullOrWhiteSpace();

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsed.Issuer.Should().Be("TriPla");
        parsed.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "alice@example.com");
        parsed.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
    }

    [Test]
    public void Constructor_ThrowsWhenSecretKeyTooShort()
    {
        var options = Options.Create(new JwtOptions { SecretKey = "short" });
        var act = () => new JwtTokenProvider(options);
        act.Should().Throw<InvalidOperationException>();
    }
}
