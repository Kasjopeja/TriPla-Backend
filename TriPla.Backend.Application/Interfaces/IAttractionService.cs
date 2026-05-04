using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.Attractions;
using TriPla.Backend.Application.DTOs.Trips;

namespace TriPla.Backend.Application.Interfaces;

public interface IAttractionService
{
    Task<Result<AttractionDto>> AddToTripAsync(Guid tripId, Guid requestingUserId, CreateAttractionRequest request, CancellationToken ct = default);
    Task<Result<AttractionDto>> UpdateAsync(Guid attractionId, Guid requestingUserId, CreateAttractionRequest request, CancellationToken ct = default);
    Task<Result<IEnumerable<AttractionDto>>> GetByTripAsync(Guid tripId, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid attractionId, Guid requestingUserId, CancellationToken ct = default);
}
