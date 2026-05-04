using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.History;

namespace TriPla.Backend.Application.Interfaces;

public interface ITripHistoryService
{
    Task<Result<IReadOnlyList<TripChangeLogDto>>> GetAsync(Guid tripId, int limit = 100, CancellationToken ct = default);
}
