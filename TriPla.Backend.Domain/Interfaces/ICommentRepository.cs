using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Domain.Interfaces;

public interface ICommentRepository : IRepository<Comment>
{
    Task<IEnumerable<Comment>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken = default);
}

