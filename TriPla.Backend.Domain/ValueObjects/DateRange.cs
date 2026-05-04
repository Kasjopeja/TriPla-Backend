namespace TriPla.Backend.Domain.ValueObjects;

public sealed class DateRange
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date must be before or equal to end date.");

        StartDate = startDate;
        EndDate = endDate;
    }

    public int DurationInDays => (EndDate - StartDate).Days;

    public bool Overlaps(DateRange other) =>
        StartDate < other.EndDate && EndDate > other.StartDate;

    public override bool Equals(object? obj) =>
        obj is DateRange other && StartDate == other.StartDate && EndDate == other.EndDate;

    public override int GetHashCode() => HashCode.Combine(StartDate, EndDate);

    public override string ToString() => $"{StartDate:yyyy-MM-dd} – {EndDate:yyyy-MM-dd}";
}

