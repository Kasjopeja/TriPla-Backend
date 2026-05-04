using FluentAssertions;
using TriPla.Backend.Domain.ValueObjects;

namespace TriPla.Backend.Tests.Domain.ValueObjects;

[TestFixture]
public class DateRangeTests
{
    [Test]
    public void Constructor_ThrowsWhenStartAfterEnd()
    {
        var act = () => new DateRange(new DateTime(2026, 5, 10), new DateTime(2026, 5, 1));
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void DurationInDays_ComputesCorrectly()
    {
        var range = new DateRange(new DateTime(2026, 5, 1), new DateTime(2026, 5, 8));
        range.DurationInDays.Should().Be(7);
    }

    [Test]
    public void Overlaps_DetectsOverlap()
    {
        var a = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10));
        var b = new DateRange(new DateTime(2026, 1, 5), new DateTime(2026, 1, 15));
        var c = new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 10));

        a.Overlaps(b).Should().BeTrue();
        a.Overlaps(c).Should().BeFalse();
    }

    [Test]
    public void Equality_UsesBothDates()
    {
        var a = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10));
        var b = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10));
        a.Should().Be(b);
    }
}
