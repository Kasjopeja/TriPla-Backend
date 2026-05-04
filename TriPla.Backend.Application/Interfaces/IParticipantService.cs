using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.Participants;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Application.Interfaces;

public interface IParticipantService
{
    Task<Result<ParticipantDto>> AddAsync(Guid tripId, Guid requestingUserId, AddParticipantRequest request, CancellationToken ct = default);
    Task<Result> RemoveAsync(Guid tripId, Guid requestingUserId, Guid userId, CancellationToken ct = default);
    Task<Result> ChangeRoleAsync(Guid tripId, Guid requestingUserId, Guid userId, ParticipantRole newRole, CancellationToken ct = default);
    Task<Result<IEnumerable<ParticipantDto>>> GetByTripAsync(Guid tripId, CancellationToken ct = default);
    Task<Result> LeaveTripAsync(Guid tripId, Guid userId, CancellationToken ct = default);
}
