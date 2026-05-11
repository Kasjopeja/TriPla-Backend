using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.History;
using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Application.Interfaces;

public interface ITripHistoryService
{
    Task<Result<IReadOnlyList<TripChangeLogDto>>> QueryAsync(ChangeLogQuery query, CancellationToken ct = default);
}
