using FluentAssertions;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Application.Trips;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Tests.Fakes;

namespace TriPla.Backend.Tests.Application;

[TestFixture]
public class TripHistoryServiceTests
{
    [Test]
    public async Task QueryAsync_ReturnsEntriesForTrip_OrderedByNewest()
    {
        var uow = new InMemoryUnitOfWork();
        var trips = new TripService(uow);
        var ownerId = Guid.NewGuid();
        var created = await trips.CreateAsync(ownerId, new CreateTripRequest(
            "Trip", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));
        await trips.UpdateAsync(created.Value!.Id, ownerId, new UpdateTripRequest(
            "Trip renamed", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));

        var history = new TripHistoryService(uow);
        var result = await history.QueryAsync(new ChangeLogQuery(created.Value.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCountGreaterThan(0);
        result.Value.First().OccurredAt.Should()
            .BeOnOrAfter(result.Value.Last().OccurredAt);
    }

    [Test]
    public async Task QueryAsync_FiltersByTripId()
    {
        var uow = new InMemoryUnitOfWork();
        var trips = new TripService(uow);
        var ownerId = Guid.NewGuid();
        var tripA = await trips.CreateAsync(ownerId, new CreateTripRequest(
            "A", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));
        var tripB = await trips.CreateAsync(ownerId, new CreateTripRequest(
            "B", null, new DateTime(2026, 2, 1), new DateTime(2026, 2, 5)));

        var history = new TripHistoryService(uow);
        var logsA = await history.QueryAsync(new ChangeLogQuery(tripA.Value!.Id));

        logsA.Value.Should().OnlyContain(e => e.TripId == tripA.Value.Id);
        logsA.Value.Should().NotContain(e => e.TripId == tripB.Value!.Id);
    }

    [Test]
    public async Task QueryAsync_RespectsLimit()
    {
        var uow = new InMemoryUnitOfWork();
        var trips = new TripService(uow);
        var ownerId = Guid.NewGuid();
        var trip = await trips.CreateAsync(ownerId, new CreateTripRequest(
            "Trip", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));
        for (var i = 0; i < 5; i++)
        {
            await trips.UpdateAsync(trip.Value!.Id, ownerId, new UpdateTripRequest(
                $"Trip v{i}", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));
        }

        var history = new TripHistoryService(uow);
        var result = await history.QueryAsync(new ChangeLogQuery(trip.Value!.Id, Limit: 2));

        result.Value.Should().HaveCount(2);
    }

    [Test]
    public async Task QueryAsync_FiltersByType()
    {
        var uow = new InMemoryUnitOfWork();
        var tripId = Guid.NewGuid();
        var actor = Guid.NewGuid();
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "TripCreated", actor, "a@x", null, new DateTime(2026, 1, 1)));
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "ExpenseAdded", actor, "a@x", null, new DateTime(2026, 1, 2)));
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "ExpenseAdded", actor, "a@x", null, new DateTime(2026, 1, 3)));

        var history = new TripHistoryService(uow);
        var result = await history.QueryAsync(new ChangeLogQuery(tripId, Type: "ExpenseAdded"));

        result.Value!.Should().HaveCount(2).And.OnlyContain(e => e.Type == "ExpenseAdded");
    }

    [Test]
    public async Task QueryAsync_FiltersByActorId()
    {
        var uow = new InMemoryUnitOfWork();
        var tripId = Guid.NewGuid();
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "X", alice, "a@x", null, new DateTime(2026, 1, 1)));
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "X", bob, "b@x", null, new DateTime(2026, 1, 2)));

        var history = new TripHistoryService(uow);
        var result = await history.QueryAsync(new ChangeLogQuery(tripId, ActorId: alice));

        result.Value!.Should().ContainSingle().Which.ActorId.Should().Be(alice);
    }

    [Test]
    public async Task QueryAsync_FiltersByDateRange()
    {
        var uow = new InMemoryUnitOfWork();
        var tripId = Guid.NewGuid();
        var actor = Guid.NewGuid();
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "X", actor, "a@x", null, new DateTime(2026, 1, 1)));
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "X", actor, "a@x", null, new DateTime(2026, 1, 5)));
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "X", actor, "a@x", null, new DateTime(2026, 1, 10)));

        var history = new TripHistoryService(uow);
        var result = await history.QueryAsync(new ChangeLogQuery(tripId,
            From: new DateTime(2026, 1, 4), To: new DateTime(2026, 1, 6)));

        result.Value!.Should().ContainSingle()
            .Which.OccurredAt.Should().Be(new DateTime(2026, 1, 5));
    }

    [Test]
    public async Task QueryAsync_SortsAscendingByOccurredAt()
    {
        var uow = new InMemoryUnitOfWork();
        var tripId = Guid.NewGuid();
        var actor = Guid.NewGuid();
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "X", actor, "a@x", null, new DateTime(2026, 1, 3)));
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "X", actor, "a@x", null, new DateTime(2026, 1, 1)));
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "X", actor, "a@x", null, new DateTime(2026, 1, 2)));

        var history = new TripHistoryService(uow);
        var result = await history.QueryAsync(new ChangeLogQuery(tripId,
            SortDirection: SortDirection.Ascending));

        result.Value!.Select(e => e.OccurredAt).Should().BeInAscendingOrder();
    }

    [Test]
    public async Task QueryAsync_SortsByType()
    {
        var uow = new InMemoryUnitOfWork();
        var tripId = Guid.NewGuid();
        var actor = Guid.NewGuid();
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "CommentAdded", actor, "a@x", null, new DateTime(2026, 1, 1)));
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "AttractionAdded", actor, "a@x", null, new DateTime(2026, 1, 2)));
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, "ExpenseAdded", actor, "a@x", null, new DateTime(2026, 1, 3)));

        var history = new TripHistoryService(uow);
        var result = await history.QueryAsync(new ChangeLogQuery(tripId,
            SortBy: ChangeLogSortField.Type, SortDirection: SortDirection.Ascending));

        result.Value!.Select(e => e.Type).Should().BeInAscendingOrder();
    }

    [Test]
    public async Task QueryAsync_AppliesSkip()
    {
        var uow = new InMemoryUnitOfWork();
        var tripId = Guid.NewGuid();
        var actor = Guid.NewGuid();
        for (var i = 0; i < 5; i++)
        {
            await uow.ChangeLog.AppendAsync(
                new TripChangeLogEntry(tripId, "X", actor, "a@x", null, new DateTime(2026, 1, 1 + i)));
        }

        var history = new TripHistoryService(uow);
        var result = await history.QueryAsync(new ChangeLogQuery(tripId, Skip: 2, Limit: 2));

        result.Value!.Should().HaveCount(2);
        // sortowanie desc — pomijamy 2 najnowsze (5, 4), oczekujemy 3 i 2
        result.Value!.Select(e => e.OccurredAt).Should().BeEquivalentTo(new[]
        {
            new DateTime(2026, 1, 3),
            new DateTime(2026, 1, 2),
        }, opts => opts.WithStrictOrdering());
    }

    [Test]
    public async Task QueryAsync_RejectsInvalidLimit()
    {
        var uow = new InMemoryUnitOfWork();
        var history = new TripHistoryService(uow);

        var tooLow = await history.QueryAsync(new ChangeLogQuery(Guid.NewGuid(), Limit: 0));
        var tooHigh = await history.QueryAsync(new ChangeLogQuery(Guid.NewGuid(), Limit: 10000));

        tooLow.IsSuccess.Should().BeFalse();
        tooHigh.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task QueryAsync_RejectsNegativeSkip()
    {
        var uow = new InMemoryUnitOfWork();
        var history = new TripHistoryService(uow);

        var result = await history.QueryAsync(new ChangeLogQuery(Guid.NewGuid(), Skip: -1));

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task QueryAsync_RejectsInvertedDateRange()
    {
        var uow = new InMemoryUnitOfWork();
        var history = new TripHistoryService(uow);

        var result = await history.QueryAsync(new ChangeLogQuery(Guid.NewGuid(),
            From: new DateTime(2026, 5, 1), To: new DateTime(2026, 1, 1)));

        result.IsSuccess.Should().BeFalse();
    }
}
