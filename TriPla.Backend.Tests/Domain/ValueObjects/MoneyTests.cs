using FluentAssertions;
using TriPla.Backend.Domain.ValueObjects;

namespace TriPla.Backend.Tests.Domain.ValueObjects;

[TestFixture]
public class MoneyTests
{
    [Test]
    public void Constructor_UppercasesCurrency()
    {
        var money = new Money(10m, "pln");
        money.Currency.Should().Be("PLN");
    }

    [Test]
    public void Constructor_ThrowsWhenAmountNegative()
    {
        var act = () => new Money(-1m, "PLN");
        act.Should().Throw<ArgumentException>();
    }

    [TestCase("")]
    [TestCase("US")]
    [TestCase("USDX")]
    public void Constructor_ThrowsOnInvalidCurrency(string currency)
    {
        var act = () => new Money(10m, currency);
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Add_SumsAmountsWhenCurrencyMatches()
    {
        var a = new Money(10m, "USD");
        var b = new Money(5m, "USD");
        a.Add(b).Should().Be(new Money(15m, "USD"));
    }

    [Test]
    public void Add_ThrowsOnMismatchedCurrencies()
    {
        var a = new Money(10m, "USD");
        var b = new Money(5m, "EUR");
        var act = () => a.Add(b);
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Subtract_ThrowsWhenResultNegative()
    {
        var a = new Money(5m, "USD");
        var b = new Money(10m, "USD");
        var act = () => a.Subtract(b);
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Equality_UsesAmountAndCurrency()
    {
        new Money(10m, "USD").Should().Be(new Money(10m, "USD"));
        new Money(10m, "USD").Should().NotBe(new Money(10m, "EUR"));
    }
}
