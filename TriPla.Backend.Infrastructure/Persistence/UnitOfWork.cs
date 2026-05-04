using TriPla.Backend.Domain.Interfaces;

namespace TriPla.Backend.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    public ITripRepository Trips { get; }
    public IUserRepository Users { get; }
    public IExpenseRepository Expenses { get; }
    public IAttractionRepository Attractions { get; }
    public ICommentRepository Comments { get; }
    public IParticipantRepository Participants { get; }
    public ITripChangeLogRepository ChangeLog { get; }

    public UnitOfWork(
        ITripRepository trips,
        IUserRepository users,
        IExpenseRepository expenses,
        IAttractionRepository attractions,
        ICommentRepository comments,
        IParticipantRepository participants,
        ITripChangeLogRepository changeLog)
    {
        Trips = trips;
        Users = users;
        Expenses = expenses;
        Attractions = attractions;
        Comments = comments;
        Participants = participants;
        ChangeLog = changeLog;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
