using JobScheduler.EntityFrameworkCore.Persistence.Context;
using JobScheduler.Storage.Abstractions.UnitOfWork;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace JobScheduler.EntityFrameworkCore.Storage
{
    internal sealed class EfUnitOfWork : IUnitOfWork
    {
        private readonly JobSchedulerDbContext _context;

        public EfUnitOfWork(JobSchedulerDbContext context)
        {
            _context = context;
        }
        public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            return new EfUnitOfWorkTransaction(transaction);
        }
    }

    internal sealed class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;
        private bool _completed;

        public EfUnitOfWorkTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            await _transaction.CommitAsync(cancellationToken);
            _completed = true;
        }

        public async Task RollbackAsync(CancellationToken cancellationToken)
        {
            await _transaction.RollbackAsync(cancellationToken);
            _completed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_completed)
            {
                // caller never explicitly committed or rolled back for safety
                // so exception wont leave a dangling open transaction
                await _transaction.RollbackAsync();
            }

            await _transaction.DisposeAsync();
        }
    }
}
