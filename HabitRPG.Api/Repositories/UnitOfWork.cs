using HabitRPG.Api.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace HabitRPG.Api.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        private IUserRepository? _users;
        private IHabitRepository? _habits;
        private ICompletionLogRepository? _completionLogs;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IUserRepository Users
        {
            get
            {
                _users ??= new UserRepository(_context);
                return _users;
            }
        }

        public IHabitRepository Habits
        {
            get
            {
                _habits ??= new HabitRepository(_context);
                return _habits;
            }
        }

        public ICompletionLogRepository CompletionLogs
        {
            get
            {
                _completionLogs ??= new CompletionLogRepository(_context);
                return _completionLogs;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}