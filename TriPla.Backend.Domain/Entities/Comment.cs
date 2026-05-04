namespace TriPla.Backend.Domain.Entities;

public class Comment
{
    public Guid Id { get; private set; }
    public Guid TripId { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Content { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? EditedAt { get; private set; }

    private Comment() { }

    public Comment(Guid tripId, Guid authorId, string content, Guid? parentId = null)
    {
        if (tripId == Guid.Empty) throw new ArgumentException("Trip ID cannot be empty.", nameof(tripId));
        if (authorId == Guid.Empty) throw new ArgumentException("Author ID cannot be empty.", nameof(authorId));
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Id = Guid.NewGuid();
        TripId = tripId;
        AuthorId = authorId;
        ParentId = parentId;
        Content = content;
        CreatedAt = DateTime.UtcNow;
    }

    public static Comment Rehydrate(Guid id, Guid tripId, Guid authorId, string content,
        DateTime createdAt, DateTime? editedAt, Guid? parentId = null)
    {
        return new Comment
        {
            Id = id,
            TripId = tripId,
            AuthorId = authorId,
            ParentId = parentId,
            Content = content,
            CreatedAt = createdAt,
            EditedAt = editedAt
        };
    }

    public void Edit(string newContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newContent);

        Content = newContent;
        EditedAt = DateTime.UtcNow;
    }
}
