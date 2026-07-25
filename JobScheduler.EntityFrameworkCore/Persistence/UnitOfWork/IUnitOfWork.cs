using Microsoft.EntityFrameworkCore.Storage;

namespace JobScheduler.Storage.EntityFrameworkCore.Persistence.UnitOfWork
{
    public interface IUnitOfWork
    {
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);
        Task CommitTransactionAsync(CancellationToken ct);
        Task RollbackTransactionAsync(CancellationToken ct);
        Task<int> SaveChangesAsync(CancellationToken ct);
    }
}
