using FluentAssertions;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.ValueObjects;

namespace TriPla.Backend.Tests.Domain.Entities;

[TestFixture]
public class TripTests
{
    private static Trip CreateTrip(Guid? ownerId = null)
    {
        var range = new DateRange(new DateTime(2026, 5, 1), new DateTime(2026, 5, 8));
        return new Trip("Summer", "Seaside", range, ownerId ?? Guid.NewGuid());
    }

    [Test]
    public void Constructor_RequiresName()
    {
        var range = new DateRange(new DateTime(2026, 5, 1), new DateTime(2026, 5, 8));
        var act = () => new Trip("", null, range, Guid.NewGuid());
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_RejectsEmptyOwner()
    {
        var range = new DateRange(new DateTime(2026, 5, 1), new DateTime(2026, 5, 8));
        var act = () => new Trip("Summer", null, range, Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void AddParticipant_AddsOnce()
    {
        var trip = CreateTrip();
        var userId = Guid.NewGuid();
        trip.AddParticipant(new Participant(trip.Id, userId));
        trip.Participants.Should().HaveCount(1);

        var act = () => trip.AddParticipant(new Participant(trip.Id, userId));
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void RemoveParticipant_WorksForNonOwner()
    {
        var trip = CreateTrip();
        var userId = Guid.NewGuid();
        trip.AddParticipant(new Participant(trip.Id, userId));

        trip.RemoveParticipant(userId);
        trip.Participants.Should().BeEmpty();
    }

    [Test]
    public void RemoveParticipant_ThrowsForOwner()
    {
        var ownerId = Guid.NewGuid();
        var trip = CreateTrip(ownerId);

        var act = () => trip.RemoveParticipant(ownerId);
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ChangeParticipantRole_UpdatesRole()
    {
        var trip = CreateTrip();
        var userId = Guid.NewGuid();
        trip.AddParticipant(new Participant(trip.Id, userId));

        trip.ChangeParticipantRole(userId, ParticipantRole.Editor);
        trip.GetRole(userId).Should().Be(ParticipantRole.Editor);
    }

    [Test]
    public void ChangeParticipantRole_ThrowsForOwner()
    {
        var ownerId = Guid.NewGuid();
        var trip = CreateTrip(ownerId);

        var act = () => trip.ChangeParticipantRole(ownerId, ParticipantRole.Member);
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Update_ChangesFieldsAndTimestamp()
    {
        var trip = CreateTrip();
        var originalUpdatedAt = trip.UpdatedAt;
        Thread.Sleep(5);
        var newRange = new DateRange(new DateTime(2026, 6, 1), new DateTime(2026, 6, 10));
        trip.Update("Renamed", "New desc", newRange);

        trip.Name.Should().Be("Renamed");
        trip.Description.Should().Be("New desc");
        trip.DateRange.Should().Be(newRange);
        trip.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }
}
