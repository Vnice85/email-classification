using EmailClassification.Application.Interfaces;
using EmailClassification.Application.Interfaces.IRepository;
using EmailClassification.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using EFCore.BulkExtensions;

namespace EmailClassification.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly EmaildbContext _context;
        private IDbContextTransaction? _transaction;
        public IEmailRepository Email { get; private set; }
        public IEmailLabelRepository EmailLabel { get; private set; }
        public IAppUserRepository AppUser { get; private set; }
        public ITokenRepository Token { get; private set; }

        public UnitOfWork(EmaildbContext context)
        {
            _context = context;
            Email = new EmailRepository(_context);
            EmailLabel = new EmailLabelRepository(_context);
            AppUser = new AppUserRepository(_context);
            Token = new TokenRepository(_context);
        }
        public async Task<bool> SaveAsync() => await _context.SaveChangesAsync() > 0;
        public async Task BeginTransactionASync()
        {
            if (_transaction == null)
            {
                _transaction = await _context.Database.BeginTransactionAsync();
            }
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
        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            await _context.DisposeAsync();
        }

        public async Task BulkInsertAsync<T>(IList<T> entities, BulkConfig? config = null) where T : class
        {
           await _context.BulkInsertAsync(entities, config);
        }

        public async Task BulkUpdateAsync<T>(IList<T> entities, BulkConfig? config = null) where T : class
        {
            await _context.BulkUpdateAsync(entities, config);

        }

        public async Task BulkDeleteAsync<T>(IList<T> entities, BulkConfig? config = null) where T : class
        {
            await _context.BulkDeleteAsync(entities, config);
        }

        public async Task BulkReadAsync<T>(IList<T> entities, BulkConfig? config = null) where T : class
        {
           await _context.BulkReadAsync(entities, config);
        }
    }
}
