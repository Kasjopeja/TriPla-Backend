using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.Comments;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Application.Interfaces;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;

namespace TriPla.Backend.Application.Comments;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;

    public CommentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CommentDto>> AddToTripAsync(Guid tripId, Guid authorId, CreateCommentRequest request, CancellationToken ct = default)
    {
        var trip = await _unitOfWork.Trips.GetByIdAsync(tripId, ct);
        if (trip is null)
            return Result.Failure<CommentDto>("Trip not found.");

        if (request.ParentId is { } parentId)
        {
            var parent = await _unitOfWork.Comments.GetByIdAsync(parentId, ct);
            if (parent is null || parent.TripId != tripId)
                return Result.Failure<CommentDto>("Parent comment not found in this trip.");
            if (parent.ParentId is not null)
                return Result.Failure<CommentDto>("Cannot reply to a reply.");
        }

        Comment comment;
        try
        {
            comment = new Comment(tripId, authorId, request.Content, request.ParentId);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CommentDto>(ex.Message);
        }

        await _unitOfWork.Comments.AddAsync(comment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _unitOfWork.AppendAsync(tripId,
            comment.ParentId is null ? "CommentAdded" : "CommentReplied",
            authorId,
            new { commentId = comment.Id, parentId = comment.ParentId, preview = Preview(comment.Content) }, ct);

        var author = await _unitOfWork.Users.GetByIdAsync(authorId, ct);
        return Result.Success(MapToDto(comment, author));
    }

    public async Task<Result<CommentDto>> UpdateAsync(Guid commentId, Guid requestingUserId, UpdateCommentRequest request, CancellationToken ct = default)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(commentId, ct);
        if (comment is null)
            return Result.Failure<CommentDto>("Comment not found.");

        if (comment.AuthorId != requestingUserId)
            return Result.Failure<CommentDto>("Only the comment author can edit the comment.");

        var beforeContent = comment.Content;

        try
        {
            comment.Edit(request.Content);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CommentDto>(ex.Message);
        }

        await _unitOfWork.Comments.UpdateAsync(comment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        if (beforeContent != comment.Content)
        {
            var changes = new Dictionary<string, object>
            {
                ["content"] = new { before = Preview(beforeContent), after = Preview(comment.Content) },
            };
            await _unitOfWork.AppendAsync(comment.TripId, "CommentUpdated", requestingUserId,
                new { commentId = comment.Id, changes }, ct);
        }

        var author = await _unitOfWork.Users.GetByIdAsync(comment.AuthorId, ct);
        return Result.Success(MapToDto(comment, author));
    }

    public async Task<Result<IEnumerable<CommentDto>>> GetByTripAsync(Guid tripId, CancellationToken ct = default)
    {
        var items = (await _unitOfWork.Comments.GetByTripIdAsync(tripId, ct)).ToList();
        if (items.Count == 0)
            return Result.Success<IEnumerable<CommentDto>>(Array.Empty<CommentDto>());

        var users = (await _unitOfWork.Users.GetByIdsAsync(items.Select(c => c.AuthorId), ct))
            .ToDictionary(u => u.Id);

        var dtos = items.Select(c => MapToDto(c, users.GetValueOrDefault(c.AuthorId)));
        return Result.Success(dtos);
    }

    public async Task<Result> DeleteAsync(Guid commentId, Guid requestingUserId, CancellationToken ct = default)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(commentId, ct);
        if (comment is null)
            return Result.Failure("Comment not found.");

        if (comment.AuthorId != requestingUserId)
            return Result.Failure("Only the comment author can delete the comment.");

        await _unitOfWork.Comments.DeleteAsync(commentId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _unitOfWork.AppendAsync(comment.TripId, "CommentDeleted", requestingUserId,
            new { commentId = comment.Id }, ct);

        return Result.Success();
    }

    private static string Preview(string content) =>
        content.Length <= 80 ? content : content[..80] + "...";

    private static CommentDto MapToDto(Comment c, User? author) =>
        new(c.Id, c.AuthorId, author?.FirstName, author?.LastName, author?.Email,
            c.ParentId, c.Content, c.CreatedAt, c.EditedAt);
}
