namespace TriPla.Backend.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ITripRepository Trips { get; }
    IUserRepository Users { get; }
    IExpenseRepository Expenses { get; }
    IAttractionRepository Attractions { get; }
    ICommentRepository Comments { get; }
    IParticipantRepository Participants { get; }
    ITripChangeLogRepository ChangeLog { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
