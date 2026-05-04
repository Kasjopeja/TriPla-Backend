using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.Participants;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Application.Interfaces;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;

namespace TriPla.Backend.Application.Participants;

public class ParticipantService : IParticipantService
{
    private readonly IUnitOfWork _unitOfWork;

    public ParticipantService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ParticipantDto>> AddAsync(Guid tripId, Guid requestingUserId, AddParticipantRequest request, CancellationToken ct = default)
    {
        var trip = await _unitOfWork.Trips.GetByIdAsync(tripId, ct);
        if (trip is null)
            return Result.Failure<ParticipantDto>("Trip not found.");

        var roleCheck = await RequireRoleAsync(tripId, requestingUserId, ParticipantRole.Editor, ct);
        if (!roleCheck.IsSuccess)
            return Result.Failure<ParticipantDto>(roleCheck.Error!);

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, ct);
        if (user is null)
            return Result.Failure<ParticipantDto>("User with the given email does not exist.");

        var existing = await _unitOfWork.Participants.GetAsync(tripId, user.Id, ct);
        if (existing is not null)
            return Result.Failure<ParticipantDto>("User is already a participant of this trip.");

        var participant = new Participant(tripId, user.Id, request.Role);
        await _unitOfWork.Participants.AddAsync(participant, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _unitOfWork.AppendAsync(tripId, "ParticipantInvited", requestingUserId,
            new { invitedEmail = user.Email, role = request.Role.ToString() }, ct);

        return Result.Success(new ParticipantDto(
            participant.Id, participant.UserId,
            user.FirstName, user.LastName, user.Email,
            participant.Role, participant.JoinedAt));
    }

    public async Task<Result> RemoveAsync(Guid tripId, Guid requestingUserId, Guid userId, CancellationToken ct = default)
    {
        var trip = await _unitOfWork.Trips.GetByIdAsync(tripId, ct);
        if (trip is null)
            return Result.Failure("Trip not found.");

        if (userId == trip.OwnerId)
            return Result.Failure("Owner cannot be removed from the trip.");

        var roleCheck = await RequireRoleAsync(tripId, requestingUserId, ParticipantRole.Editor, ct);
        if (!roleCheck.IsSuccess)
            return roleCheck;

        var participant = await _unitOfWork.Participants.GetAsync(tripId, userId, ct);
        if (participant is null)
            return Result.Failure("Participant not found.");

        await _unitOfWork.Participants.DeleteAsync(tripId, userId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var removed = await _unitOfWork.Users.GetByIdAsync(userId, ct);
        await _unitOfWork.AppendAsync(tripId, "ParticipantRemoved", requestingUserId,
            new { removedEmail = removed?.Email, removedUserId = userId }, ct);

        return Result.Success();
    }

    public async Task<Result> ChangeRoleAsync(Guid tripId, Guid requestingUserId, Guid userId, ParticipantRole newRole, CancellationToken ct = default)
    {
        var trip = await _unitOfWork.Trips.GetByIdAsync(tripId, ct);
        if (trip is null)
            return Result.Failure("Trip not found.");

        if (userId == trip.OwnerId)
            return Result.Failure("Cannot change the role of the trip owner.");

        if (requestingUserId != trip.OwnerId)
            return Result.Failure("Only the trip owner can change participant roles.");

        var participant = await _unitOfWork.Participants.GetAsync(tripId, userId, ct);
        if (participant is null)
            return Result.Failure("Participant not found.");

        participant.ChangeRole(newRole);
        await _unitOfWork.Participants.UpdateAsync(participant, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var target = await _unitOfWork.Users.GetByIdAsync(userId, ct);
        await _unitOfWork.AppendAsync(tripId, "RoleChanged", requestingUserId,
            new { targetEmail = target?.Email, newRole = newRole.ToString() }, ct);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<ParticipantDto>>> GetByTripAsync(Guid tripId, CancellationToken ct = default)
    {
        var items = (await _unitOfWork.Participants.GetByTripIdAsync(tripId, ct)).ToList();
        if (items.Count == 0)
            return Result.Success<IEnumerable<ParticipantDto>>(Array.Empty<ParticipantDto>());

        var users = (await _unitOfWork.Users.GetByIdsAsync(items.Select(p => p.UserId), ct))
            .ToDictionary(u => u.Id);

        var dtos = items.Select(p =>
        {
            var u = users.GetValueOrDefault(p.UserId);
            return new ParticipantDto(p.Id, p.UserId, u?.FirstName, u?.LastName, u?.Email, p.Role, p.JoinedAt);
        });

        return Result.Success(dtos);
    }

    public async Task<Result> LeaveTripAsync(Guid tripId, Guid userId, CancellationToken ct = default)
    {
        var trip = await _unitOfWork.Trips.GetByIdAsync(tripId, ct);
        if (trip is null)
            return Result.Failure("Trip not found.");

        if (userId == trip.OwnerId)
            return Result.Failure("Owner cannot leave their own trip. Transfer ownership or delete the trip.");

        var participant = await _unitOfWork.Participants.GetAsync(tripId, userId, ct);
        if (participant is null)
            return Result.Failure("You are not a participant of this trip.");

        await _unitOfWork.Participants.DeleteAsync(tripId, userId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, ct);
        await _unitOfWork.AppendAsync(tripId, "ParticipantLeft", userId,
            new { email = user?.Email }, ct);

        return Result.Success();
    }

    private async Task<Result> RequireRoleAsync(Guid tripId, Guid userId, ParticipantRole minRole, CancellationToken ct)
    {
        var p = await _unitOfWork.Participants.GetAsync(tripId, userId, ct);
        if (p is null)
            return Result.Failure("You are not a participant of this trip.");
        if ((int)p.Role < (int)minRole)
            return Result.Failure("You do not have permission to perform this action.");
        return Result.Success();
    }
}
