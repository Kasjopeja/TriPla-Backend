using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.Trips;

namespace TriPla.Backend.Application.Interfaces;

public interface ITripService
{
    Task<Result<TripDto>> CreateAsync(Guid ownerId, CreateTripRequest request, CancellationToken ct = default);
    Task<Result<TripDto>> UpdateAsync(Guid tripId, Guid requestingUserId, UpdateTripRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid tripId, Guid requestingUserId, CancellationToken ct = default);
    Task<Result<TripDetailsDto>> GetByIdAsync(Guid tripId, CancellationToken ct = default);
    Task<Result<IEnumerable<TripDto>>> GetByUserAsync(Guid userId, CancellationToken ct = default);
}
