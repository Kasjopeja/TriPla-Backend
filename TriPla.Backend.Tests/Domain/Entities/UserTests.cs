using FluentAssertions;
using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Tests.Domain.Entities;

[TestFixture]
public class UserTests
{
    [Test]
    public void Constructor_NormalizesEmailToLowercase()
    {
        var user = new User("Alice", "Smith", "Alice@Example.COM", "hash");
        user.Email.Should().Be("alice@example.com");
    }

    [Test]
    public void Constructor_RequiresEmailAtSign()
    {
        var act = () => new User("Alice", "Smith", "no-at-sign", "hash");
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void UpdateProfile_ChangesNames()
    {
        var user = new User("Alice", "Smith", "a@b.com", "hash");
        user.UpdateProfile("Bob", "Jones");
        user.FirstName.Should().Be("Bob");
        user.LastName.Should().Be("Jones");
    }
}
