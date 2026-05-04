using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Application.DTOs.Trips;

public record TripDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    Guid OwnerId,
    List<ParticipantDto> Participants,
    List<AttractionDto> Attractions,
    List<ExpenseDto> Expenses,
    List<CommentDto> Comments,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ParticipantDto(
    Guid Id,
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? Email,
    ParticipantRole Role,
    DateTime JoinedAt);

public record AttractionDto(
    Guid Id,
    string Name,
    string? Description,
    string? Street,
    string? City,
    string? Country,
    DateTime? PlannedAt);

public record ExpenseDto(
    Guid Id,
    Guid PaidByUserId,
    string? PayerFirstName,
    string? PayerLastName,
    string? PayerEmail,
    string Title,
    string? Description,
    decimal Amount,
    string Currency,
    ExpenseCategory Category,
    DateTime Date,
    bool IsSettled,
    List<ExpenseSplitDto> Splits);

public record ExpenseSplitDto(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? Email,
    decimal Amount,
    string Currency);

public record CommentDto(
    Guid Id,
    Guid AuthorId,
    string? AuthorFirstName,
    string? AuthorLastName,
    string? AuthorEmail,
    Guid? ParentId,
    string Content,
    DateTime CreatedAt,
    DateTime? EditedAt);
