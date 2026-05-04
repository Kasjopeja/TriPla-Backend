using FluentAssertions;
using TriPla.Backend.Domain.ValueObjects;

namespace TriPla.Backend.Tests.Domain.ValueObjects;

[TestFixture]
public class AddressTests
{
    [Test]
    public void Constructor_RejectsBlankStreet()
    {
        var act = () => new Address("", "Warsaw", "Poland");
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void ToString_IncludesPostalCode_WhenProvided()
    {
        var address = new Address("Main 1", "Warsaw", "Poland", "00-001");
        address.ToString().Should().Contain("00-001");
    }

    [Test]
    public void Equality_ComparesAllFields()
    {
        var a = new Address("Main 1", "Warsaw", "Poland");
        var b = new Address("Main 1", "Warsaw", "Poland");
        var c = new Address("Other 1", "Warsaw", "Poland");
        a.Should().Be(b);
        a.Should().NotBe(c);
    }
}
