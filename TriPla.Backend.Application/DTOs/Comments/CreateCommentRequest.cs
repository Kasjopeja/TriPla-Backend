namespace TriPla.Backend.Application.DTOs.Comments;

public record CreateCommentRequest(string Content, Guid? ParentId = null);

public record UpdateCommentRequest(string Content);
