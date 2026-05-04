using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;

namespace TriPla.Backend.Tests.Fakes;

public class InMemoryUnitOfWork : IUnitOfWork
{
    public InMemoryParticipantRepository ParticipantsStore { get; } = new();
    public InMemoryTripRepository TripsStore { get; }
    public InMemoryUserRepository UsersStore { get; } = new();
    public InMemoryAttractionRepository AttractionsStore { get; } = new();
    public InMemoryExpenseRepository ExpensesStore { get; } = new();
    public InMemoryCommentRepository CommentsStore { get; } = new();
    public InMemoryTripChangeLogRepository ChangeLogStore { get; } = new();

    public InMemoryUnitOfWork()
    {
        TripsStore = new InMemoryTripRepository(ParticipantsStore);
    }

    public ITripRepository Trips => TripsStore;
    public IUserRepository Users => UsersStore;
    public IExpenseRepository Expenses => ExpensesStore;
    public IAttractionRepository Attractions => AttractionsStore;
    public ICommentRepository Comments => CommentsStore;
    public IParticipantRepository Participants => ParticipantsStore;
    public ITripChangeLogRepository ChangeLog => ChangeLogStore;

    public int SaveChangesCalls { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalls++;
        return Task.FromResult(1);
    }

    public void Dispose() { }
}

public class InMemoryTripRepository : ITripRepository
{
    private readonly InMemoryParticipantRepository _participants;
    public Dictionary<Guid, Trip> Store { get; } = new();

    public InMemoryTripRepository(InMemoryParticipantRepository participants)
    {
        _participants = participants;
    }

    public Task<Trip?> GetByIdAsync(Guid id, CancellationToken _ = default) =>
        Task.FromResult(Store.TryGetValue(id, out var t) ? t : null);

    public Task<Trip?> GetWithDetailsAsync(Guid id, CancellationToken _ = default) =>
        GetByIdAsync(id);

    public Task<IEnumerable<Trip>> GetAllAsync(CancellationToken _ = default) =>
        Task.FromResult<IEnumerable<Trip>>(Store.Values.ToList());

    public Task<IEnumerable<Trip>> GetByOwnerIdAsync(Guid ownerId, CancellationToken _ = default) =>
        Task.FromResult<IEnumerable<Trip>>(Store.Values.Where(t => t.OwnerId == ownerId).ToList());

    public Task<IEnumerable<Trip>> GetByParticipantIdAsync(Guid userId, CancellationToken _ = default) =>
        Task.FromResult<IEnumerable<Trip>>(Store.Values.Where(t => t.HasParticipant(userId)).ToList());

    public Task AddAsync(Trip entity, CancellationToken _ = default)
    {
        Store[entity.Id] = entity;
        foreach (var p in entity.Participants)
        {
            if (_participants.Store.All(x => x.Id != p.Id))
                _participants.Store.Add(p);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Trip entity, CancellationToken _ = default)
    {
        Store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken _ = default)
    {
        Store.Remove(id);
        return Task.CompletedTask;
    }
}

public class InMemoryUserRepository : IUserRepository
{
    public Dictionary<Guid, User> Store { get; } = new();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken _ = default) =>
        Task.FromResult(Store.TryGetValue(id, out var u) ? u : null);

    public Task<User?> GetByEmailAsync(string email, CancellationToken _ = default) =>
        Task.FromResult(Store.Values.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken _ = default) =>
        Task.FromResult(Store.Values.Any(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken _ = default)
    {
        var set = ids.ToHashSet();
        return Task.FromResult<IEnumerable<User>>(Store.Values.Where(u => set.Contains(u.Id)).ToList());
    }

    public Task<IEnumerable<User>> GetAllAsync(CancellationToken _ = default) =>
        Task.FromResult<IEnumerable<User>>(Store.Values.ToList());

    public Task AddAsync(User entity, CancellationToken _ = default)
    {
        Store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User entity, CancellationToken _ = default)
    {
        Store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken _ = default)
    {
        Store.Remove(id);
        return Task.CompletedTask;
    }
}

public class InMemoryAttractionRepository : IAttractionRepository
{
    public Dictionary<Guid, Attraction> Store { get; } = new();

    public Task<Attraction?> GetByIdAsync(Guid id, CancellationToken _ = default) =>
        Task.FromResult(Store.TryGetValue(id, out var a) ? a : null);

    public Task<IEnumerable<Attraction>> GetByTripIdAsync(Guid tripId, CancellationToken _ = default) =>
        Task.FromResult<IEnumerable<Attraction>>(Store.Values.Where(a => a.TripId == tripId).ToList());

    public Task<IEnumerable<Attraction>> GetAllAsync(CancellationToken _ = default) =>
        Task.FromResult<IEnumerable<Attraction>>(Store.Values.ToList());

    public Task AddAsync(Attraction entity, CancellationToken _ = default)
    {
        Store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Attraction entity, CancellationToken _ = default)
    {
        Store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken _ = default)
    {
        Store.Remove(id);
        return Task.CompletedTask;
    }
}

public class InMemoryExpenseRepository : IExpenseRepository
{
    public Dictionary<Guid, Expense> Store { get; } = new();

    public Task<Expense?> GetByIdAsync(Guid id, CancellationToken _ = default) =>
        Task.FromResult(Store.TryGetValue(id, out var e) ? e : null);

    public Task<IEnumerable<Expense>> GetByTripIdAsync(Guid tripId, CancellationToken _ = default) =>
        Task.FromResult<IEnumerable<Expense>>(Store.Values.Where(e => e.TripId == tripId).ToList());

    public Task<IEnumerable<Expense>> GetByPaidByUserIdAsync(Guid userId, CancellationToken _ = default) =>
        Task.FromResult<IEnumerable<Expense>>(Store.Values.Where(e => e.PaidByUserId == userId).ToList());

    public Task<IEnumerable<Expense>> GetAllAsync(CancellationToken _ = default) =>
        Task.FromResult<IEnumerable<Expense>>(Store.Values.ToList());

    public Task AddAsync(Expense entity, CancellationToken _ = default)
    {
        Store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Expense entity, CancellationToken _ = default)
    {
        Store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken _ = default)
    {
        Store.Remove(id);
        return Task.CompletedTask;
    }

    public Task SetSettledAsync(Guid expenseId, bool isSettled, CancellationToken _ = default)
    {
        if (Store.TryGetValue(expenseId, out var e))
            e.SetSettled(isSettled);
        return Task.CompletedTask;
    }

    public Task<int> SetAllSettledAsync(Guid tripId, bool isSettled, CancellationToken _ = default)
    {
        var changed = 0;
        foreach (var e in Store.Values.Where(x => x.TripId == tripId && x.IsSettled != isSettled))
        {
            e.SetSettled(isSettled);
            changed++;
        }
        return Task.FromResult(changed);
    }
}

public class InMemoryCommentRepository : ICommentRepository
{
    public Dictionary<Guid, Comment> Store { get; } = new();

    public Task<Comment?> GetByIdAsync(Guid id, CancellationToken _ = default) =>
        Task.FromResult(Store.TryGetValue(id, out var c) ? c : null);

    public Task<IEnumerable<Comment>> GetByTripIdAsync(Guid tripId, CancellationToken _ = default) =>
        Task.FromResult<IEnumerable<Comment>>(Store.Values.Where(c => c.TripId == tripId).ToList());

    public Task<IEnumerable<Comment>> GetAllAsync(CancellationToken _ = default) =>
        Task.FromResult<IEnumerable<Comment>>(Store.Values.ToList());

    public Task AddAsync(Comment entity, CancellationToken _ = default)
    {
        Store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Comment entity, CancellationToken _ = default)
    {
        Store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken _ = default)
    {
        Store.Remove(id);
        return Task.CompletedTask;
    }
}

public class InMemoryTripChangeLogRepository : ITripChangeLogRepository
{
    public List<TripChangeLogEntry> Store { get; } = new();

    public Task AppendAsync(TripChangeLogEntry entry, CancellationToken _ = default)
    {
        Store.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TripChangeLogEntry>> GetByTripIdAsync(Guid tripId, int limit = 100, CancellationToken _ = default)
    {
        IReadOnlyList<TripChangeLogEntry> list = Store
            .Where(e => e.TripId == tripId)
            .OrderByDescending(e => e.OccurredAt)
            .Take(limit)
            .ToList();
        return Task.FromResult(list);
    }
}

public class InMemoryParticipantRepository : IParticipantRepository
{
    public List<Participant> Store { get; } = new();

    public Task<Participant?> GetAsync(Guid tripId, Guid userId, CancellationToken _ = default) =>
        Task.FromResult(Store.FirstOrDefault(p => p.TripId == tripId && p.UserId == userId));

    public Task<IEnumerable<Participant>> GetByTripIdAsync(Guid tripId, CancellationToken _ = default) =>
        Task.FromResult<IEnumerable<Participant>>(Store.Where(p => p.TripId == tripId).ToList());

    public Task<IReadOnlyDictionary<Guid, int>> GetCountsByTripIdsAsync(IEnumerable<Guid> tripIds, CancellationToken _ = default)
    {
        var set = tripIds.ToHashSet();
        var dict = (IReadOnlyDictionary<Guid, int>)Store
            .Where(p => set.Contains(p.TripId))
            .GroupBy(p => p.TripId)
            .ToDictionary(g => g.Key, g => g.Count());
        return Task.FromResult(dict);
    }

    public Task AddAsync(Participant participant, CancellationToken _ = default)
    {
        Store.Add(participant);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Participant participant, CancellationToken _ = default)
    {
        var existing = Store.FirstOrDefault(p => p.Id == participant.Id);
        if (existing is not null) Store.Remove(existing);
        Store.Add(participant);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid tripId, Guid userId, CancellationToken _ = default)
    {
        Store.RemoveAll(p => p.TripId == tripId && p.UserId == userId);
        return Task.CompletedTask;
    }
}
