using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Application.Interfaces;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;
using TriPla.Backend.Domain.ValueObjects;

namespace TriPla.Backend.Application.Trips;

public class TripService : ITripService
{
    private readonly IUnitOfWork _unitOfWork;

    public TripService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TripDto>> CreateAsync(Guid ownerId, CreateTripRequest request, CancellationToken ct = default)
    {
        Trip trip;
        try
        {
            var dateRange = new DateRange(request.StartDate, request.EndDate);
            trip = new Trip(request.Name, request.Description, dateRange, ownerId);

            var owner = new Participant(trip.Id, ownerId, ParticipantRole.Organizer);
            trip.AddParticipant(owner);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<TripDto>(ex.Message);
        }

        await _unitOfWork.Trips.AddAsync(trip, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _unitOfWork.AppendAsync(trip.Id, "TripCreated", ownerId, new { name = trip.Name }, ct);

        return Result.Success(MapToDto(trip, trip.Participants.Count));
    }

    public async Task<Result<TripDto>> UpdateAsync(Guid tripId, Guid requestingUserId, UpdateTripRequest request, CancellationToken ct = default)
    {
        var trip = await _unitOfWork.Trips.GetByIdAsync(tripId, ct);
        if (trip is null)
            return Result.Failure<TripDto>("Trip not found.");

        var roleCheck = await RequireRoleAsync(tripId, requestingUserId, ParticipantRole.Editor, ct);
        if (!roleCheck.IsSuccess)
            return Result.Failure<TripDto>(roleCheck.Error!);

        var beforeName = trip.Name;
        var beforeDescription = trip.Description;
        var beforeStart = trip.DateRange.StartDate;
        var beforeEnd = trip.DateRange.EndDate;

        try
        {
            var dateRange = new DateRange(request.StartDate, request.EndDate);
            trip.Update(request.Name, request.Description, dateRange);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<TripDto>(ex.Message);
        }

        var changes = new Dictionary<string, object>();
        if (beforeName != trip.Name)
            changes["name"] = new { before = beforeName, after = trip.Name };
        if (beforeDescription != trip.Description)
            changes["description"] = new { before = beforeDescription, after = trip.Description };
        if (beforeStart != trip.DateRange.StartDate)
            changes["startDate"] = new { before = beforeStart, after = trip.DateRange.StartDate };
        if (beforeEnd != trip.DateRange.EndDate)
            changes["endDate"] = new { before = beforeEnd, after = trip.DateRange.EndDate };

        await _unitOfWork.Trips.UpdateAsync(trip, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        if (changes.Count > 0)
        {
            await _unitOfWork.AppendAsync(trip.Id, "TripUpdated", requestingUserId,
                new { changes }, ct);
        }

        var counts = await _unitOfWork.Participants.GetCountsByTripIdsAsync(new[] { trip.Id }, ct);
        return Result.Success(MapToDto(trip, counts.GetValueOrDefault(trip.Id, 0)));
    }

    public async Task<Result> DeleteAsync(Guid tripId, Guid requestingUserId, CancellationToken ct = default)
    {
        var trip = await _unitOfWork.Trips.GetByIdAsync(tripId, ct);
        if (trip is null)
            return Result.Failure("Trip not found.");

        if (trip.OwnerId != requestingUserId)
            return Result.Failure("Only the trip owner can delete the trip.");

        await _unitOfWork.Trips.DeleteAsync(tripId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _unitOfWork.AppendAsync(tripId, "TripDeleted", requestingUserId, new { name = trip.Name }, ct);

        return Result.Success();
    }

    public async Task<Result<TripDetailsDto>> GetByIdAsync(Guid tripId, CancellationToken ct = default)
    {
        var trip = await _unitOfWork.Trips.GetWithDetailsAsync(tripId, ct);
        if (trip is null)
            return Result.Failure<TripDetailsDto>("Trip not found.");

        var userIds = CollectUserIds(trip);
        var users = (await _unitOfWork.Users.GetByIdsAsync(userIds, ct)).ToDictionary(u => u.Id);

        return Result.Success(MapToDetailsDto(trip, users));
    }

    public async Task<Result<IEnumerable<TripDto>>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var owned = await _unitOfWork.Trips.GetByOwnerIdAsync(userId, ct);
        var participated = await _unitOfWork.Trips.GetByParticipantIdAsync(userId, ct);

        var all = owned.Union(participated, new TripIdComparer()).ToList();
        if (all.Count == 0)
            return Result.Success<IEnumerable<TripDto>>(Array.Empty<TripDto>());

        var counts = await _unitOfWork.Participants.GetCountsByTripIdsAsync(
            all.Select(t => t.Id), ct);

        var dtos = all
            .Select(t => MapToDto(t, counts.GetValueOrDefault(t.Id, 0)))
            .ToList();

        return Result.Success<IEnumerable<TripDto>>(dtos);
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

    private static IEnumerable<Guid> CollectUserIds(Trip trip)
    {
        var ids = new HashSet<Guid> { trip.OwnerId };
        foreach (var p in trip.Participants) ids.Add(p.UserId);
        foreach (var e in trip.Expenses)
        {
            ids.Add(e.PaidByUserId);
            foreach (var s in e.Splits) ids.Add(s.UserId);
        }
        foreach (var c in trip.Comments) ids.Add(c.AuthorId);
        return ids;
    }

    private static TripDto MapToDto(Trip trip, int participantCount) => new(
        trip.Id,
        trip.Name,
        trip.Description,
        trip.DateRange.StartDate,
        trip.DateRange.EndDate,
        trip.OwnerId,
        participantCount,
        trip.CreatedAt,
        trip.UpdatedAt);

    private static TripDetailsDto MapToDetailsDto(Trip trip, IReadOnlyDictionary<Guid, User> users) => new(
        trip.Id,
        trip.Name,
        trip.Description,
        trip.DateRange.StartDate,
        trip.DateRange.EndDate,
        trip.OwnerId,
        trip.Participants.Select(p =>
        {
            var u = users.GetValueOrDefault(p.UserId);
            return new ParticipantDto(p.Id, p.UserId, u?.FirstName, u?.LastName, u?.Email, p.Role, p.JoinedAt);
        }).ToList(),
        trip.Attractions.Select(a =>
            new AttractionDto(a.Id, a.Name, a.Description,
                a.Address?.Street, a.Address?.City, a.Address?.Country, a.PlannedAt)).ToList(),
        trip.Expenses.Select(e =>
        {
            var payer = users.GetValueOrDefault(e.PaidByUserId);
            var splits = e.Splits.Select(s =>
            {
                var u = users.GetValueOrDefault(s.UserId);
                return new ExpenseSplitDto(s.UserId, u?.FirstName, u?.LastName, u?.Email,
                    s.Amount.Amount, s.Amount.Currency);
            }).ToList();
            return new ExpenseDto(e.Id, e.PaidByUserId, payer?.FirstName, payer?.LastName, payer?.Email,
                e.Title, e.Description, e.Amount.Amount, e.Amount.Currency, e.Category, e.Date,
                e.IsSettled, splits);
        }).ToList(),
        trip.Comments.Select(c =>
        {
            var u = users.GetValueOrDefault(c.AuthorId);
            return new CommentDto(c.Id, c.AuthorId, u?.FirstName, u?.LastName, u?.Email,
                c.ParentId, c.Content, c.CreatedAt, c.EditedAt);
        }).ToList(),
        trip.CreatedAt,
        trip.UpdatedAt);

    private class TripIdComparer : IEqualityComparer<Trip>
    {
        public bool Equals(Trip? x, Trip? y) => x?.Id == y?.Id;
        public int GetHashCode(Trip obj) => obj.Id.GetHashCode();
    }
}
