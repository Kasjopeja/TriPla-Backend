using FluentAssertions;
using TriPla.Backend.Application.Comments;
using TriPla.Backend.Application.DTOs.Comments;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Application.Trips;
using TriPla.Backend.Tests.Fakes;

namespace TriPla.Backend.Tests.Application;

[TestFixture]
public class CommentServiceTests
{
    private static async Task<(Guid tripId, Guid userId)> SeedAsync(InMemoryUnitOfWork uow)
    {
        var userId = Guid.NewGuid();
        var trips = new TripService(uow);
        var trip = await trips.CreateAsync(userId, new CreateTripRequest(
            "Trip", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 10)));
        return (trip.Value!.Id, userId);
    }

    [Test]
    public async Task AddToTripAsync_PersistsComment()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, userId) = await SeedAsync(uow);

        var service = new CommentService(uow);
        var result = await service.AddToTripAsync(tripId, userId, new CreateCommentRequest("Hello"));

        result.IsSuccess.Should().BeTrue();
        uow.CommentsStore.Store.Should().HaveCount(1);
    }

    [Test]
    public async Task AddReply_SucceedsForTopLevelParent()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, userId) = await SeedAsync(uow);
        var service = new CommentService(uow);

        var parent = await service.AddToTripAsync(tripId, userId, new CreateCommentRequest("Parent"));
        var reply = await service.AddToTripAsync(tripId, userId,
            new CreateCommentRequest("Reply", parent.Value!.Id));

        reply.IsSuccess.Should().BeTrue();
        uow.CommentsStore.Store.Values.Should()
            .Contain(c => c.ParentId == parent.Value.Id);
    }

    [Test]
    public async Task AddReply_FailsForReplyToReply()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, userId) = await SeedAsync(uow);
        var service = new CommentService(uow);

        var parent = await service.AddToTripAsync(tripId, userId, new CreateCommentRequest("Parent"));
        var reply = await service.AddToTripAsync(tripId, userId,
            new CreateCommentRequest("Reply", parent.Value!.Id));

        var nested = await service.AddToTripAsync(tripId, userId,
            new CreateCommentRequest("Nested", reply.Value!.Id));

        nested.IsSuccess.Should().BeFalse();
        nested.Error.Should().Contain("reply");
    }

    [Test]
    public async Task AddReply_FailsWhenParentInDifferentTrip()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripA, userA) = await SeedAsync(uow);
        var trips = new TripService(uow);
        var tripB = (await trips.CreateAsync(userA, new CreateTripRequest(
            "Other", null, new DateTime(2026, 2, 1), new DateTime(2026, 2, 5)))).Value!.Id;

        var service = new CommentService(uow);
        var parent = await service.AddToTripAsync(tripA, userA, new CreateCommentRequest("ParentA"));

        var reply = await service.AddToTripAsync(tripB, userA,
            new CreateCommentRequest("Reply", parent.Value!.Id));

        reply.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task UpdateAsync_OnlyAuthorCanEdit()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, userId) = await SeedAsync(uow);
        var service = new CommentService(uow);
        var added = await service.AddToTripAsync(tripId, userId, new CreateCommentRequest("Hello"));
        var commentId = added.Value!.Id;

        var notAuthor = Guid.NewGuid();
        var denied = await service.UpdateAsync(commentId, notAuthor, new UpdateCommentRequest("Hijacked"));
        denied.IsSuccess.Should().BeFalse();

        var allowed = await service.UpdateAsync(commentId, userId, new UpdateCommentRequest("Updated"));
        allowed.IsSuccess.Should().BeTrue();
        uow.CommentsStore.Store[commentId].Content.Should().Be("Updated");
    }

    [Test]
    public async Task UpdateAsync_LogsContentDiff()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, userId) = await SeedAsync(uow);
        var service = new CommentService(uow);
        var added = await service.AddToTripAsync(tripId, userId, new CreateCommentRequest("Hello"));

        await service.UpdateAsync(added.Value!.Id, userId, new UpdateCommentRequest("Hello world"));

        var log = uow.ChangeLogStore.Store.Single(e => e.Type == "CommentUpdated");
        log.PayloadJson.Should().Contain("\"content\"");
        log.PayloadJson.Should().Contain("\"before\"");
        log.PayloadJson.Should().Contain("\"after\"");
    }

    [Test]
    public async Task DeleteAsync_OnlyAuthorCanDelete()
    {
        var uow = new InMemoryUnitOfWork();
        var (tripId, userId) = await SeedAsync(uow);
        var service = new CommentService(uow);

        var added = await service.AddToTripAsync(tripId, userId, new CreateCommentRequest("Hello"));
        var commentId = added.Value!.Id;

        var otherUser = Guid.NewGuid();
        var deleteByOther = await service.DeleteAsync(commentId, otherUser);
        deleteByOther.IsSuccess.Should().BeFalse();

        var deleteByAuthor = await service.DeleteAsync(commentId, userId);
        deleteByAuthor.IsSuccess.Should().BeTrue();
    }
}
