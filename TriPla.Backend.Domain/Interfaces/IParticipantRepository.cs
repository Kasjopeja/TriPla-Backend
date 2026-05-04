using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Domain.Interfaces;

public interface IParticipantRepository
{
    Task<Participant?> GetAsync(Guid tripId, Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Participant>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> GetCountsByTripIdsAsync(IEnumerable<Guid> tripIds, CancellationToken cancellationToken = default);
    Task AddAsync(Participant participant, CancellationToken cancellationToken = default);
    Task UpdateAsync(Participant participant, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tripId, Guid userId, CancellationToken cancellationToken = default);
}
