using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.Expenses;
using TriPla.Backend.Application.DTOs.Trips;

namespace TriPla.Backend.Application.Interfaces;

public interface IExpenseService
{
    Task<Result<ExpenseDto>> AddToTripAsync(Guid tripId, Guid paidByUserId, CreateExpenseRequest request, CancellationToken ct = default);
    Task<Result<ExpenseDto>> UpdateAsync(Guid expenseId, Guid requestingUserId, UpdateExpenseRequest request, CancellationToken ct = default);
    Task<Result<ExpenseDto>> SetSettledAsync(Guid expenseId, Guid requestingUserId, bool isSettled, CancellationToken ct = default);
    Task<Result<int>> SetAllSettledAsync(Guid tripId, Guid requestingUserId, bool isSettled, CancellationToken ct = default);
    Task<Result<IEnumerable<ExpenseDto>>> GetByTripAsync(Guid tripId, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid expenseId, Guid requestingUserId, CancellationToken ct = default);
}
