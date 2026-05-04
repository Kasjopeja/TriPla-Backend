using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.Attractions;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Application.Interfaces;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;
using TriPla.Backend.Domain.ValueObjects;

namespace TriPla.Backend.Application.Attractions;

public class AttractionService : IAttractionService
{
    private readonly IUnitOfWork _unitOfWork;

    public AttractionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AttractionDto>> AddToTripAsync(Guid tripId, Guid requestingUserId, CreateAttractionRequest request, CancellationToken ct = default)
    {
        var trip = await _unitOfWork.Trips.GetByIdAsync(tripId, ct);
        if (trip is null)
            return Result.Failure<AttractionDto>("Trip not found.");

        var roleCheck = await RequireRoleAsync(tripId, requestingUserId, ParticipantRole.Editor, ct);
        if (!roleCheck.IsSuccess)
            return Result.Failure<AttractionDto>(roleCheck.Error!);

        var address = BuildAddress(request);
        var attraction = new Attraction(tripId, request.Name, request.Description, address, request.PlannedAt);

        await _unitOfWork.Attractions.AddAsync(attraction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _unitOfWork.AppendAsync(tripId, "AttractionAdded", requestingUserId,
            new { name = attraction.Name, attractionId = attraction.Id }, ct);

        return Result.Success(MapToDto(attraction));
    }

    public async Task<Result<AttractionDto>> UpdateAsync(Guid attractionId, Guid requestingUserId, CreateAttractionRequest request, CancellationToken ct = default)
    {
        var attraction = await _unitOfWork.Attractions.GetByIdAsync(attractionId, ct);
        if (attraction is null)
            return Result.Failure<AttractionDto>("Attraction not found.");

        var roleCheck = await RequireRoleAsync(attraction.TripId, requestingUserId, ParticipantRole.Editor, ct);
        if (!roleCheck.IsSuccess)
            return Result.Failure<AttractionDto>(roleCheck.Error!);

        var beforeName = attraction.Name;
        var beforeDescription = attraction.Description;
        var beforeStreet = attraction.Address?.Street;
        var beforeCity = attraction.Address?.City;
        var beforeCountry = attraction.Address?.Country;
        var beforePlannedAt = attraction.PlannedAt;

        var address = BuildAddress(request);
        attraction.Update(request.Name, request.Description, address, request.PlannedAt);

        var changes = new Dictionary<string, object>();
        if (beforeName != attraction.Name)
            changes["name"] = new { before = beforeName, after = attraction.Name };
        if (beforeDescription != attraction.Description)
            changes["description"] = new { before = beforeDescription, after = attraction.Description };
        if (beforeStreet != attraction.Address?.Street)
            changes["street"] = new { before = beforeStreet, after = attraction.Address?.Street };
        if (beforeCity != attraction.Address?.City)
            changes["city"] = new { before = beforeCity, after = attraction.Address?.City };
        if (beforeCountry != attraction.Address?.Country)
            changes["country"] = new { before = beforeCountry, after = attraction.Address?.Country };
        if (beforePlannedAt != attraction.PlannedAt)
            changes["plannedAt"] = new { before = beforePlannedAt, after = attraction.PlannedAt };

        await _unitOfWork.Attractions.UpdateAsync(attraction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        if (changes.Count > 0)
        {
            await _unitOfWork.AppendAsync(attraction.TripId, "AttractionUpdated", requestingUserId,
                new { attractionId = attraction.Id, name = attraction.Name, changes }, ct);
        }

        return Result.Success(MapToDto(attraction));
    }

    public async Task<Result<IEnumerable<AttractionDto>>> GetByTripAsync(Guid tripId, CancellationToken ct = default)
    {
        var items = await _unitOfWork.Attractions.GetByTripIdAsync(tripId, ct);
        return Result.Success(items.Select(MapToDto));
    }

    public async Task<Result> DeleteAsync(Guid attractionId, Guid requestingUserId, CancellationToken ct = default)
    {
        var attraction = await _unitOfWork.Attractions.GetByIdAsync(attractionId, ct);
        if (attraction is null)
            return Result.Failure("Attraction not found.");

        var roleCheck = await RequireRoleAsync(attraction.TripId, requestingUserId, ParticipantRole.Editor, ct);
        if (!roleCheck.IsSuccess)
            return roleCheck;

        await _unitOfWork.Attractions.DeleteAsync(attractionId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _unitOfWork.AppendAsync(attraction.TripId, "AttractionDeleted", requestingUserId,
            new { name = attraction.Name, attractionId = attraction.Id }, ct);

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

    private static Address? BuildAddress(CreateAttractionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Street) ||
            string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.Country))
            return null;

        return new Address(request.Street, request.City, request.Country);
    }

    private static AttractionDto MapToDto(Attraction a) => new(
        a.Id,
        a.Name,
        a.Description,
        a.Address?.Street,
        a.Address?.City,
        a.Address?.Country,
        a.PlannedAt);
}
