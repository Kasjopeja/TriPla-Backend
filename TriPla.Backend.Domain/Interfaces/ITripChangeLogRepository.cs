using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Domain.Interfaces;

public interface ITripChangeLogRepository
{
    Task AppendAsync(TripChangeLogEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TripChangeLogEntry>> GetByTripIdAsync(Guid tripId, int limit = 100, CancellationToken cancellationToken = default);
}
