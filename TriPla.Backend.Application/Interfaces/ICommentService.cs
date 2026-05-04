using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.Comments;
using TriPla.Backend.Application.DTOs.Trips;

namespace TriPla.Backend.Application.Interfaces;

public interface ICommentService
{
    Task<Result<CommentDto>> AddToTripAsync(Guid tripId, Guid authorId, CreateCommentRequest request, CancellationToken ct = default);
    Task<Result<CommentDto>> UpdateAsync(Guid commentId, Guid requestingUserId, UpdateCommentRequest request, CancellationToken ct = default);
    Task<Result<IEnumerable<CommentDto>>> GetByTripAsync(Guid tripId, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid commentId, Guid requestingUserId, CancellationToken ct = default);
}
