using EFCore.BulkExtensions;
using EmailClassification.Application.Interfaces.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.Interfaces
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IEmailRepository Email { get; }
        IEmailLabelRepository EmailLabel { get; }
        IAppUserRepository AppUser { get; }
        ITokenRepository Token { get; }
        Task<bool> SaveAsync();

        Task BulkInsertAsync<T>(IList<T> entities, BulkConfig? config = null) where T : class;
        Task BulkUpdateAsync<T>(IList<T> entities, BulkConfig? config = null) where T : class;
        Task BulkDeleteAsync<T>(IList<T> entities, BulkConfig? config = null) where T : class;
        Task BulkReadAsync<T>(IList<T> entities, BulkConfig? config = null) where T : class;

        Task BeginTransactionASync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

    }
}
