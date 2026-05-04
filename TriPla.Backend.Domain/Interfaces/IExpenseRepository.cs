using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Domain.Interfaces;

public interface IExpenseRepository : IRepository<Expense>
{
    Task<IEnumerable<Expense>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Expense>> GetByPaidByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetSettledAsync(Guid expenseId, bool isSettled, CancellationToken cancellationToken = default);
    Task<int> SetAllSettledAsync(Guid tripId, bool isSettled, CancellationToken cancellationToken = default);
}
