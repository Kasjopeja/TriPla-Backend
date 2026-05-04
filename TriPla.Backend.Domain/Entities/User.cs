namespace TriPla.Backend.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public User(string firstName, string lastName, string email, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        if (!email.Contains('@'))
            throw new ArgumentException("Invalid email format.", nameof(email));

        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Email = email.ToLowerInvariant();
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }

    public static User Rehydrate(Guid id, string firstName, string lastName, string email,
        string passwordHash, DateTime createdAt)
    {
        return new User
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = createdAt
        };
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        FirstName = firstName;
        LastName = lastName;
    }
}
