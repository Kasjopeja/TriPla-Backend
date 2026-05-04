using FluentAssertions;
using TriPla.Backend.Application.DTOs.Participants;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Application.Participants;
using TriPla.Backend.Application.Trips;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Tests.Fakes;

namespace TriPla.Backend.Tests.Application;

[TestFixture]
public class ParticipantServiceTests
{
    private static async Task<(Guid tripId, Guid ownerId, Guid inviteeId)> SeedAsync(InMemoryUnitOfWork uow)
    {
        var owner = new User("Alice", "A", "alice@example.com", "hash");
        var invitee = new User("Bob", "B", "bob@example.com", "hash");
        await uow.Users.AddAsync(owner);
        await uow.Users.AddAsync(invitee);

        var trips = new TripService(uow);
        var trip = await trips.CreateAsync(owner.Id, new CreateTripRequest(
            "Trip", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 10)));

        return (trip.Value!.Id, owner.Id, invitee.Id);
    }

    [Test]
    public async Task AddAsync_InvitesByEmail()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, ownerId, _) = await SeedAsync(uow);

        var service = new ParticipantService(uow);
        var result = await service.AddAsync(tripId, ownerId, new AddParticipantRequest("bob@example.com"));

        result.IsSuccess.Should().BeTrue();
        uow.ParticipantsStore.Store.Should().HaveCount(2);
    }

    [Test]
    public async Task AddAsync_FailsForUnknownEmail()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, ownerId, _) = await SeedAsync(uow);

        var service = new ParticipantService(uow);
        var result = await service.AddAsync(tripId, ownerId, new AddParticipantRequest("ghost@example.com"));

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task AddAsync_FailsIfAlreadyParticipant()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, ownerId, inviteeId) = await SeedAsync(uow);

        await uow.Participants.AddAsync(new Participant(tripId, inviteeId));

        var service = new ParticipantService(uow);
        var result = await service.AddAsync(tripId, ownerId, new AddParticipantRequest("bob@example.com"));

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task AddAsync_FailsWhenRequesterIsNotEditor()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, _, _) = await SeedAsync(uow);

        var outsider = new User("Eve", "E", "eve@example.com", "hash");
        await uow.Users.AddAsync(outsider);
        await uow.Participants.AddAsync(new Participant(tripId, outsider.Id, ParticipantRole.Member));

        var service = new ParticipantService(uow);
        var result = await service.AddAsync(tripId, outsider.Id, new AddParticipantRequest("bob@example.com"));

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task RemoveAsync_CannotRemoveOwner()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, ownerId, _) = await SeedAsync(uow);

        var service = new ParticipantService(uow);
        var result = await service.RemoveAsync(tripId, ownerId, ownerId);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task LeaveTripAsync_OwnerCannotLeave()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, ownerId, _) = await SeedAsync(uow);

        var service = new ParticipantService(uow);
        var result = await service.LeaveTripAsync(tripId, ownerId);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task LeaveTripAsync_RemovesParticipant()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, _, inviteeId) = await SeedAsync(uow);
        await uow.Participants.AddAsync(new Participant(tripId, inviteeId));

        var service = new ParticipantService(uow);
        var result = await service.LeaveTripAsync(tripId, inviteeId);

        result.IsSuccess.Should().BeTrue();
        uow.ParticipantsStore.Store.Should().NotContain(p => p.UserId == inviteeId);
    }

    [Test]
    public async Task ChangeRoleAsync_OwnerRoleImmutable()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, ownerId, _) = await SeedAsync(uow);

        var service = new ParticipantService(uow);
        var result = await service.ChangeRoleAsync(tripId, ownerId, ownerId, ParticipantRole.Member);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task ChangeRoleAsync_FailsWhenRequesterNotOwner()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, _, inviteeId) = await SeedAsync(uow);
        await uow.Participants.AddAsync(new Participant(tripId, inviteeId, ParticipantRole.Editor));

        var service = new ParticipantService(uow);
        var result = await service.ChangeRoleAsync(tripId, inviteeId, inviteeId, ParticipantRole.Member);

        result.IsSuccess.Should().BeFalse();
    }
}
