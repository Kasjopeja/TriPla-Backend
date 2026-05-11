using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.History;
using TriPla.Backend.Application.Interfaces;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;

namespace TriPla.Backend.Application.Trips;

public class TripHistoryService : ITripHistoryService
{
    private const int MaxLimit = 500;

    private readonly IUnitOfWork _unitOfWork;

    public TripHistoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<TripChangeLogDto>>> QueryAsync(ChangeLogQuery query, CancellationToken ct = default)
    {
        if (query.Limit is <= 0 or > MaxLimit)
            return Result.Failure<IReadOnlyList<TripChangeLogDto>>($"limit must be between 1 and {MaxLimit}.");
        if (query.Skip < 0)
            return Result.Failure<IReadOnlyList<TripChangeLogDto>>("skip must be >= 0.");
        if (query.From is { } from && query.To is { } to && from > to)
            return Result.Failure<IReadOnlyList<TripChangeLogDto>>("'from' must be <= 'to'.");

        var entries = await _unitOfWork.ChangeLog.QueryAsync(query, ct);
        IReadOnlyList<TripChangeLogDto> dtos = entries
            .Select(e => new TripChangeLogDto(e.TripId, e.Type, e.ActorId, e.ActorEmail, e.PayloadJson, e.OccurredAt))
            .ToList();
        return Result.Success(dtos);
    }
}
