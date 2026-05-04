using FluentAssertions;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Application.Trips;
using TriPla.Backend.Tests.Fakes;

namespace TriPla.Backend.Tests.Application;

[TestFixture]
public class TripHistoryServiceTests
{
    [Test]
    public async Task GetAsync_ReturnsEntriesForTrip_OrderedByNewest()
    {
        var uow = new InMemoryUnitOfWork();
        var trips = new TripService(uow);
        var ownerId = Guid.NewGuid();
        var created = await trips.CreateAsync(ownerId, new CreateTripRequest(
            "Trip", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));
        await trips.UpdateAsync(created.Value!.Id, ownerId, new UpdateTripRequest(
            "Trip renamed", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));

        var history = new TripHistoryService(uow);
        var result = await history.GetAsync(created.Value.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCountGreaterThan(0);
        result.Value.First().OccurredAt.Should()
            .BeOnOrAfter(result.Value.Last().OccurredAt);
    }

    [Test]
    public async Task GetAsync_FiltersByTripId()
    {
        var uow = new InMemoryUnitOfWork();
        var trips = new TripService(uow);
        var ownerId = Guid.NewGuid();
        var tripA = await trips.CreateAsync(ownerId, new CreateTripRequest(
            "A", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));
        var tripB = await trips.CreateAsync(ownerId, new CreateTripRequest(
            "B", null, new DateTime(2026, 2, 1), new DateTime(2026, 2, 5)));

        var history = new TripHistoryService(uow);
        var logsA = await history.GetAsync(tripA.Value!.Id);

        logsA.Value.Should().OnlyContain(e => e.TripId == tripA.Value.Id);
        logsA.Value.Should().NotContain(e => e.TripId == tripB.Value!.Id);
    }

    [Test]
    public async Task GetAsync_RespectsLimit()
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
        var result = await history.GetAsync(trip.Value!.Id, limit: 2);

        result.Value.Should().HaveCount(2);
    }
}
