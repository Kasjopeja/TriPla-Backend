using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Domain.Interfaces;

public interface ITripRepository : IRepository<Trip>
{
    Task<IEnumerable<Trip>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Trip>> GetByParticipantIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Trip?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}

