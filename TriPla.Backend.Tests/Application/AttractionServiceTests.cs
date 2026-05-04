using FluentAssertions;
using TriPla.Backend.Application.Attractions;
using TriPla.Backend.Application.DTOs.Attractions;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Application.Trips;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Tests.Fakes;

namespace TriPla.Backend.Tests.Application;

[TestFixture]
public class AttractionServiceTests
{
    private static async Task<(Guid tripId, Guid ownerId)> SeedTripAsync(InMemoryUnitOfWork uow)
    {
        var ownerId = Guid.NewGuid();
        var trips = new TripService(uow);
        var trip = await trips.CreateAsync(ownerId, new CreateTripRequest(
            "Trip", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 10)));
        return (trip.Value!.Id, ownerId);
    }

    [Test]
    public async Task AddToTripAsync_AddsWithoutAddressWhenFieldsMissing()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, ownerId) = await SeedTripAsync(uow);
        var service = new AttractionService(uow);

        var result = await service.AddToTripAsync(tripId, ownerId, new CreateAttractionRequest(
            "Museum", null, null, null, null, new DateTime(2026, 1, 2)));

        result.IsSuccess.Should().BeTrue();
        uow.AttractionsStore.Store.Values.Single().Address.Should().BeNull();
    }

    [Test]
    public async Task AddToTripAsync_AddsAddressWhenFullyProvided()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, ownerId) = await SeedTripAsync(uow);
        var service = new AttractionService(uow);

        var result = await service.AddToTripAsync(tripId, ownerId, new CreateAttractionRequest(
            "Louvre", null, "Rue de Rivoli", "Paris", "France", new DateTime(2026, 1, 2)));

        result.IsSuccess.Should().BeTrue();
        var stored = uow.AttractionsStore.Store.Values.Single();
        stored.Address.Should().NotBeNull();
        stored.Address!.City.Should().Be("Paris");
    }

    [Test]
    public async Task AddToTripAsync_FailsWhenTripMissing()
    {
        var uow = new InMemoryUnitOfWork();
        var service = new AttractionService(uow);

        var result = await service.AddToTripAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateAttractionRequest(
            "X", null, null, null, null, null));

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task UpdateAsync_SucceedsForOwner()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, ownerId) = await SeedTripAsync(uow);
        var service = new AttractionService(uow);

        var added = await service.AddToTripAsync(tripId, ownerId, new CreateAttractionRequest(
            "Museum", null, null, null, null, new DateTime(2026, 1, 2)));

        var updated = await service.UpdateAsync(added.Value!.Id, ownerId, new CreateAttractionRequest(
            "Museum (renamed)", "new desc", "Street", "City", "PL", new DateTime(2026, 1, 3)));

        updated.IsSuccess.Should().BeTrue();
        uow.AttractionsStore.Store.Values.Single().Name.Should().Be("Museum (renamed)");
    }

    [Test]
    public async Task UpdateAsync_FailsForMember()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, ownerId) = await SeedTripAsync(uow);
        var memberId = Guid.NewGuid();
        await uow.Participants.AddAsync(new Participant(tripId, memberId, ParticipantRole.Member));
        var service = new AttractionService(uow);

        var added = await service.AddToTripAsync(tripId, ownerId, new CreateAttractionRequest(
            "Museum", null, null, null, null, new DateTime(2026, 1, 2)));

        var updated = await service.UpdateAsync(added.Value!.Id, memberId, new CreateAttractionRequest(
            "Hijacked", null, null, null, null, null));

        updated.IsSuccess.Should().BeFalse();
        uow.AttractionsStore.Store.Values.Single().Name.Should().Be("Museum");
    }

    [Test]
    public async Task UpdateAsync_LogsDiffOfChangedFields()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, ownerId) = await SeedTripAsync(uow);
        var service = new AttractionService(uow);

        var added = await service.AddToTripAsync(tripId, ownerId, new CreateAttractionRequest(
            "Museum", "old", null, null, null, new DateTime(2026, 1, 2)));

        await service.UpdateAsync(added.Value!.Id, ownerId, new CreateAttractionRequest(
            "Museum", "new", null, null, null, new DateTime(2026, 1, 2)));

        var log = uow.ChangeLogStore.Store.Single(e => e.Type == "AttractionUpdated");
        log.PayloadJson.Should().Contain("\"description\"");
        log.PayloadJson.Should().NotContain("\"name\":{");
    }

    [Test]
    public async Task DeleteAsync_FailsForMember()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, ownerId) = await SeedTripAsync(uow);
        var memberId = Guid.NewGuid();
        await uow.Participants.AddAsync(new Participant(tripId, memberId, ParticipantRole.Member));
        var service = new AttractionService(uow);

        var added = await service.AddToTripAsync(tripId, ownerId, new CreateAttractionRequest(
            "Museum", null, null, null, null, new DateTime(2026, 1, 2)));

        var deleted = await service.DeleteAsync(added.Value!.Id, memberId);
        deleted.IsSuccess.Should().BeFalse();
        uow.AttractionsStore.Store.Should().HaveCount(1);
    }

    [Test]
    public async Task AddToTripAsync_FailsWhenRequesterIsMemberOnly()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, _) = await SeedTripAsync(uow);
        var memberId = Guid.NewGuid();
        await uow.Participants.AddAsync(new Participant(tripId, memberId, ParticipantRole.Member));

        var service = new AttractionService(uow);
        var result = await service.AddToTripAsync(tripId, memberId, new CreateAttractionRequest(
            "X", null, null, null, null, null));

        result.IsSuccess.Should().BeFalse();
        uow.AttractionsStore.Store.Should().BeEmpty();
    }
}
