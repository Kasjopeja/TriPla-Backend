namespace TriPla.Backend.Domain.ValueObjects;

public sealed class Address
{
    public string Street { get; }
    public string City { get; }
    public string Country { get; }
    public string? PostalCode { get; }

    public Address(string street, string city, string country, string? postalCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);

        Street = street;
        City = city;
        Country = country;
        PostalCode = postalCode;
    }

    public override bool Equals(object? obj) =>
        obj is Address other &&
        Street == other.Street &&
        City == other.City &&
        Country == other.Country &&
        PostalCode == other.PostalCode;

    public override int GetHashCode() => HashCode.Combine(Street, City, Country, PostalCode);

    public override string ToString() =>
        string.IsNullOrEmpty(PostalCode)
            ? $"{Street}, {City}, {Country}"
            : $"{Street}, {PostalCode} {City}, {Country}";
}
