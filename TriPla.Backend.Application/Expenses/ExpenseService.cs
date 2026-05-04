using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.DTOs.Expenses;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Application.Interfaces;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;
using TriPla.Backend.Domain.ValueObjects;

namespace TriPla.Backend.Application.Expenses;

public class ExpenseService : IExpenseService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExpenseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ExpenseDto>> AddToTripAsync(Guid tripId, Guid paidByUserId, CreateExpenseRequest request, CancellationToken ct = default)
    {
        var trip = await _unitOfWork.Trips.GetByIdAsync(tripId, ct);
        if (trip is null)
            return Result.Failure<ExpenseDto>("Trip not found.");

        Money money;
        Expense expense;
        try
        {
            money = new Money(request.Amount, request.Currency);
            expense = new Expense(tripId, paidByUserId, request.Title, request.Description, money, request.Category, request.Date);

            if (request.Splits is { Count: > 0 })
            {
                foreach (var split in request.Splits)
                {
                    var splitMoney = new Money(split.Amount, request.Currency);
                    expense.AddSplit(new ExpenseSplit(expense.Id, split.UserId, splitMoney));
                }
                expense.ValidateSplitsSum();
            }
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ExpenseDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ExpenseDto>(ex.Message);
        }

        await _unitOfWork.Expenses.AddAsync(expense, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _unitOfWork.AppendAsync(tripId, "ExpenseAdded", paidByUserId,
            new { title = expense.Title, amount = expense.Amount.Amount, currency = expense.Amount.Currency, expenseId = expense.Id }, ct);

        var userIds = new HashSet<Guid> { paidByUserId };
        foreach (var s in expense.Splits) userIds.Add(s.UserId);
        var users = (await _unitOfWork.Users.GetByIdsAsync(userIds, ct)).ToDictionary(u => u.Id);

        return Result.Success(MapToDto(expense, users));
    }

    public async Task<Result<ExpenseDto>> UpdateAsync(Guid expenseId, Guid requestingUserId, UpdateExpenseRequest request, CancellationToken ct = default)
    {
        var expense = await _unitOfWork.Expenses.GetByIdAsync(expenseId, ct);
        if (expense is null)
            return Result.Failure<ExpenseDto>("Expense not found.");

        if (expense.PaidByUserId != requestingUserId)
            return Result.Failure<ExpenseDto>("Only the payer can edit the expense.");

        var beforeTitle = expense.Title;
        var beforeDescription = expense.Description;
        var beforeAmount = expense.Amount.Amount;
        var beforeCurrency = expense.Amount.Currency;
        var beforeCategory = expense.Category;
        var beforeDate = expense.Date;
        var beforeSplitsSig = SplitsSignature(expense.Splits);

        try
        {
            var money = new Money(request.Amount, request.Currency);
            expense.Update(request.Title, request.Description, money, request.Category, request.Date);

            expense.ClearSplits();
            if (request.Splits is { Count: > 0 })
            {
                foreach (var split in request.Splits)
                {
                    var splitMoney = new Money(split.Amount, request.Currency);
                    expense.AddSplit(new ExpenseSplit(expense.Id, split.UserId, splitMoney));
                }
                expense.ValidateSplitsSum();
            }
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ExpenseDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ExpenseDto>(ex.Message);
        }

        var changes = new Dictionary<string, object>();
        if (beforeTitle != expense.Title)
            changes["title"] = new { before = beforeTitle, after = expense.Title };
        if (beforeDescription != expense.Description)
            changes["description"] = new { before = beforeDescription, after = expense.Description };
        if (beforeAmount != expense.Amount.Amount)
            changes["amount"] = new { before = beforeAmount, after = expense.Amount.Amount };
        if (beforeCurrency != expense.Amount.Currency)
            changes["currency"] = new { before = beforeCurrency, after = expense.Amount.Currency };
        if (beforeCategory != expense.Category)
            changes["category"] = new { before = beforeCategory.ToString(), after = expense.Category.ToString() };
        if (beforeDate != expense.Date)
            changes["date"] = new { before = beforeDate, after = expense.Date };
        var afterSplitsSig = SplitsSignature(expense.Splits);
        if (beforeSplitsSig != afterSplitsSig)
            changes["splits"] = new { before = beforeSplitsSig, after = afterSplitsSig };

        await _unitOfWork.Expenses.UpdateAsync(expense, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        if (changes.Count > 0)
        {
            await _unitOfWork.AppendAsync(expense.TripId, "ExpenseUpdated", requestingUserId,
                new { expenseId = expense.Id, title = expense.Title, changes }, ct);
        }

        var userIds = new HashSet<Guid> { expense.PaidByUserId };
        foreach (var s in expense.Splits) userIds.Add(s.UserId);
        var users = (await _unitOfWork.Users.GetByIdsAsync(userIds, ct)).ToDictionary(u => u.Id);

        return Result.Success(MapToDto(expense, users));
    }

    public async Task<Result<ExpenseDto>> SetSettledAsync(Guid expenseId, Guid requestingUserId, bool isSettled, CancellationToken ct = default)
    {
        var expense = await _unitOfWork.Expenses.GetByIdAsync(expenseId, ct);
        if (expense is null)
            return Result.Failure<ExpenseDto>("Expense not found.");

        var participant = await _unitOfWork.Participants.GetAsync(expense.TripId, requestingUserId, ct);
        if (participant is null)
            return Result.Failure<ExpenseDto>("You are not a participant of this trip.");

        if (expense.IsSettled == isSettled)
        {
            var noopUsers = await ResolveExpenseUsers(expense, ct);
            return Result.Success(MapToDto(expense, noopUsers));
        }

        await _unitOfWork.Expenses.SetSettledAsync(expenseId, isSettled, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        expense.SetSettled(isSettled);

        await _unitOfWork.AppendAsync(expense.TripId,
            isSettled ? "ExpenseSettled" : "ExpenseUnsettled",
            requestingUserId,
            new { expenseId = expense.Id, title = expense.Title }, ct);

        var users = await ResolveExpenseUsers(expense, ct);
        return Result.Success(MapToDto(expense, users));
    }

    public async Task<Result<int>> SetAllSettledAsync(Guid tripId, Guid requestingUserId, bool isSettled, CancellationToken ct = default)
    {
        var trip = await _unitOfWork.Trips.GetByIdAsync(tripId, ct);
        if (trip is null)
            return Result.Failure<int>("Trip not found.");

        var participant = await _unitOfWork.Participants.GetAsync(tripId, requestingUserId, ct);
        if (participant is null || (int)participant.Role < (int)ParticipantRole.Editor)
            return Result.Failure<int>("Only trip editors and the owner can settle all expenses at once.");

        var changed = await _unitOfWork.Expenses.SetAllSettledAsync(tripId, isSettled, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        if (changed > 0)
        {
            await _unitOfWork.AppendAsync(tripId,
                isSettled ? "AllExpensesSettled" : "AllExpensesUnsettled",
                requestingUserId,
                new { count = changed }, ct);
        }

        return Result.Success(changed);
    }

    private async Task<IReadOnlyDictionary<Guid, User>> ResolveExpenseUsers(Expense expense, CancellationToken ct)
    {
        var ids = new HashSet<Guid> { expense.PaidByUserId };
        foreach (var s in expense.Splits) ids.Add(s.UserId);
        return (await _unitOfWork.Users.GetByIdsAsync(ids, ct)).ToDictionary(u => u.Id);
    }

    public async Task<Result<IEnumerable<ExpenseDto>>> GetByTripAsync(Guid tripId, CancellationToken ct = default)
    {
        var expenses = (await _unitOfWork.Expenses.GetByTripIdAsync(tripId, ct)).ToList();
        if (expenses.Count == 0)
            return Result.Success<IEnumerable<ExpenseDto>>(Array.Empty<ExpenseDto>());

        var userIds = new HashSet<Guid>();
        foreach (var e in expenses)
        {
            userIds.Add(e.PaidByUserId);
            foreach (var s in e.Splits) userIds.Add(s.UserId);
        }

        var users = (await _unitOfWork.Users.GetByIdsAsync(userIds, ct)).ToDictionary(u => u.Id);
        return Result.Success(expenses.Select(e => MapToDto(e, users)));
    }

    public async Task<Result> DeleteAsync(Guid expenseId, Guid requestingUserId, CancellationToken ct = default)
    {
        var expense = await _unitOfWork.Expenses.GetByIdAsync(expenseId, ct);
        if (expense is null)
            return Result.Failure("Expense not found.");

        if (expense.PaidByUserId != requestingUserId)
            return Result.Failure("Only the payer can delete the expense.");

        await _unitOfWork.Expenses.DeleteAsync(expenseId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _unitOfWork.AppendAsync(expense.TripId, "ExpenseDeleted", requestingUserId,
            new { title = expense.Title, expenseId = expense.Id }, ct);

        return Result.Success();
    }

    private static string SplitsSignature(IEnumerable<ExpenseSplit> splits) =>
        string.Join(";", splits
            .OrderBy(s => s.UserId)
            .Select(s => $"{s.UserId}:{s.Amount.Amount:0.##}{s.Amount.Currency}"));

    private static ExpenseDto MapToDto(Expense e, IReadOnlyDictionary<Guid, User> users)
    {
        var payer = users.GetValueOrDefault(e.PaidByUserId);
        var splits = e.Splits.Select(s =>
        {
            var u = users.GetValueOrDefault(s.UserId);
            return new ExpenseSplitDto(s.UserId, u?.FirstName, u?.LastName, u?.Email,
                s.Amount.Amount, s.Amount.Currency);
        }).ToList();

        return new ExpenseDto(
            e.Id, e.PaidByUserId, payer?.FirstName, payer?.LastName, payer?.Email,
            e.Title, e.Description, e.Amount.Amount, e.Amount.Currency, e.Category, e.Date,
            e.IsSettled, splits);
    }
}
