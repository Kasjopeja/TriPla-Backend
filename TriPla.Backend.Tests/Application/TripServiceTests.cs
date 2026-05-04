using FluentAssertions;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Application.Trips;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Tests.Fakes;

namespace TriPla.Backend.Tests.Application;

[TestFixture]
public class TripServiceTests
{
    [Test]
    public async Task CreateAsync_PersistsTripAndOwnerParticipant()
    {
        var uow = new InMemoryUnitOfWork();
        var service = new TripService(uow);
        var ownerId = Guid.NewGuid();

        var result = await service.CreateAsync(ownerId, new CreateTripRequest(
            "Paris", "City break",
            new DateTime(2026, 6, 1), new DateTime(2026, 6, 5)));

        result.IsSuccess.Should().BeTrue();
        uow.TripsStore.Store.Should().HaveCount(1);
        var trip = uow.TripsStore.Store.Values.Single();
        trip.OwnerId.Should().Be(ownerId);
        trip.Participants.Should().ContainSingle(p => p.UserId == ownerId);
    }

    [Test]
    public async Task CreateAsync_ReturnsFailureWhenStartAfterEnd()
    {
        var uow = new InMemoryUnitOfWork();
        var service = new TripService(uow);

        var result = await service.CreateAsync(Guid.NewGuid(), new CreateTripRequest(
            "Bad", null,
            new DateTime(2026, 6, 10), new DateTime(2026, 6, 1)));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("before or equal to end date");
    }

    [Test]
    public async Task UpdateAsync_ReturnsFailureWhenTripNotFound()
    {
        var uow = new InMemoryUnitOfWork();
        var service = new TripService(uow);

        var result = await service.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateTripRequest(
            "X", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 2)));

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task DeleteAsync_RemovesExistingTrip()
    {
        var uow = new InMemoryUnitOfWork();
        var service = new TripService(uow);
        var ownerId = Guid.NewGuid();

        var create = await service.CreateAsync(ownerId, new CreateTripRequest(
            "Rome", null, new DateTime(2026, 6, 1), new DateTime(2026, 6, 5)));

        var tripId = create.Value!.Id;
        var delete = await service.DeleteAsync(tripId, ownerId);

        delete.IsSuccess.Should().BeTrue();
        uow.TripsStore.Store.Should().BeEmpty();
    }

    [Test]
    public async Task UpdateAsync_SucceedsWhenRequesterIsOwner()
    {
        var uow = new InMemoryUnitOfWork();
        var service = new TripService(uow);
        var ownerId = Guid.NewGuid();
        var create = await service.CreateAsync(ownerId, new CreateTripRequest(
            "Original", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));

        var result = await service.UpdateAsync(create.Value!.Id, ownerId, new UpdateTripRequest(
            "Renamed", "new", new DateTime(2026, 1, 2), new DateTime(2026, 1, 6)));

        result.IsSuccess.Should().BeTrue();
        uow.TripsStore.Store.Values.Single().Name.Should().Be("Renamed");
    }

    [Test]
    public async Task UpdateAsync_FailsWhenRequesterIsMemberOnly()
    {
        var uow = new InMemoryUnitOfWork();
        var service = new TripService(uow);
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var create = await service.CreateAsync(ownerId, new CreateTripRequest(
            "Trip", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));
        await uow.Participants.AddAsync(new Participant(create.Value!.Id, memberId, ParticipantRole.Member));

        var result = await service.UpdateAsync(create.Value.Id, memberId, new UpdateTripRequest(
            "Renamed", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("permission");
    }

    [Test]
    public async Task UpdateAsync_LogsOnlyChangedFields()
    {
        var uow = new InMemoryUnitOfWork();
        var service = new TripService(uow);
        var ownerId = Guid.NewGuid();
        var create = await service.CreateAsync(ownerId, new CreateTripRequest(
            "Original", "Old desc", new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));

        await service.UpdateAsync(create.Value!.Id, ownerId, new UpdateTripRequest(
            "Renamed", "Old desc", new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));

        var logs = uow.ChangeLogStore.Store.Where(e => e.Type == "TripUpdated").ToList();
        logs.Should().HaveCount(1);
        logs[0].PayloadJson.Should().Contain("\"name\"");
        logs[0].PayloadJson.Should().NotContain("\"description\"");
        logs[0].PayloadJson.Should().NotContain("\"startDate\"");
    }

    [Test]
    public async Task UpdateAsync_SkipsLogWhenNothingChanged()
    {
        var uow = new InMemoryUnitOfWork();
        var service = new TripService(uow);
        var ownerId = Guid.NewGuid();
        var create = await service.CreateAsync(ownerId, new CreateTripRequest(
            "Same", "Same", new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));

        await service.UpdateAsync(create.Value!.Id, ownerId, new UpdateTripRequest(
            "Same", "Same", new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));

        uow.ChangeLogStore.Store.Should().NotContain(e => e.Type == "TripUpdated");
    }

    [Test]
    public async Task DeleteAsync_FailsWhenRequesterIsNotOwner()
    {
        var uow = new InMemoryUnitOfWork();
        var service = new TripService(uow);
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var create = await service.CreateAsync(ownerId, new CreateTripRequest(
            "Trip", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));
        await uow.Participants.AddAsync(new Participant(create.Value!.Id, editorId, ParticipantRole.Editor));

        var result = await service.DeleteAsync(create.Value.Id, editorId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("owner");
        uow.TripsStore.Store.Should().HaveCount(1);
    }

    [Test]
    public async Task GetByUserAsync_ReturnsOwnedTrips()
    {
        var uow = new InMemoryUnitOfWork();
        var service = new TripService(uow);
        var userId = Guid.NewGuid();

        await service.CreateAsync(userId, new CreateTripRequest(
            "A", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 5)));
        await service.CreateAsync(userId, new CreateTripRequest(
            "B", null, new DateTime(2026, 2, 1), new DateTime(2026, 2, 5)));

        var result = await service.GetByUserAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
