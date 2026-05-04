using FluentAssertions;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.ValueObjects;

namespace TriPla.Backend.Tests.Domain.Entities;

[TestFixture]
public class ExpenseTests
{
    private static Expense CreateExpense(decimal amount = 100m, string currency = "PLN")
    {
        return new Expense(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Hotel",
            null,
            new Money(amount, currency),
            ExpenseCategory.Accommodation,
            new DateTime(2026, 5, 1));
    }

    [Test]
    public void Constructor_RejectsZeroOrNegativeAmount()
    {
        var act = () => CreateExpense(0m);
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void AddSplit_RejectsMismatchedCurrency()
    {
        var expense = CreateExpense(100m, "PLN");
        var split = new ExpenseSplit(expense.Id, Guid.NewGuid(), new Money(50m, "EUR"));
        var act = () => expense.AddSplit(split);
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ValidateSplitsSum_PassesWhenSplitsMatchTotal()
    {
        var expense = CreateExpense(100m, "PLN");
        expense.AddSplit(new ExpenseSplit(expense.Id, Guid.NewGuid(), new Money(60m, "PLN")));
        expense.AddSplit(new ExpenseSplit(expense.Id, Guid.NewGuid(), new Money(40m, "PLN")));

        var act = () => expense.ValidateSplitsSum();
        act.Should().NotThrow();
    }

    [Test]
    public void ValidateSplitsSum_ThrowsWhenSplitsDoNotMatchTotal()
    {
        var expense = CreateExpense(100m, "PLN");
        expense.AddSplit(new ExpenseSplit(expense.Id, Guid.NewGuid(), new Money(60m, "PLN")));
        expense.AddSplit(new ExpenseSplit(expense.Id, Guid.NewGuid(), new Money(30m, "PLN")));

        var act = () => expense.ValidateSplitsSum();
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ValidateSplitsSum_IsNoOpWhenNoSplits()
    {
        var expense = CreateExpense();
        var act = () => expense.ValidateSplitsSum();
        act.Should().NotThrow();
    }
}
