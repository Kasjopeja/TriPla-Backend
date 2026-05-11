namespace TriPla.Backend.Domain.Entities;

public enum ChangeLogSortField
{
    OccurredAt,
    Type,
    ActorEmail
}

public enum SortDirection
{
    Ascending,
    Descending
}

public record ChangeLogQuery(
    Guid TripId,
    string? Type = null,
    Guid? ActorId = null,
    DateTime? From = null,
    DateTime? To = null,
    ChangeLogSortField SortBy = ChangeLogSortField.OccurredAt,
    SortDirection SortDirection = SortDirection.Descending,
    int Skip = 0,
    int Limit = 100);
