using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.History;
using TriPla.Backend.Application.Interfaces;
using TriPla.Backend.Domain.Interfaces;

namespace TriPla.Backend.Application.Trips;

public class TripHistoryService : ITripHistoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public TripHistoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<TripChangeLogDto>>> GetAsync(Guid tripId, int limit = 100, CancellationToken ct = default)
    {
        var entries = await _unitOfWork.ChangeLog.GetByTripIdAsync(tripId, limit, ct);
        IReadOnlyList<TripChangeLogDto> dtos = entries
            .Select(e => new TripChangeLogDto(e.TripId, e.Type, e.ActorId, e.ActorEmail, e.PayloadJson, e.OccurredAt))
            .ToList();
        return Result.Success(dtos);
    }
}
