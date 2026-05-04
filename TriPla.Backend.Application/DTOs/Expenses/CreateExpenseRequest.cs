using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Application.DTOs.Expenses;

public record CreateExpenseRequest(
    string Title,
    string? Description,
    decimal Amount,
    string Currency,
    ExpenseCategory Category,
    DateTime Date,
    List<ExpenseSplitRequest>? Splits);

public record ExpenseSplitRequest(
    Guid UserId,
    decimal Amount);

public record UpdateExpenseRequest(
    string Title,
    string? Description,
    decimal Amount,
    string Currency,
    ExpenseCategory Category,
    DateTime Date,
    List<ExpenseSplitRequest>? Splits);

public record SetSettledRequest(bool IsSettled);

