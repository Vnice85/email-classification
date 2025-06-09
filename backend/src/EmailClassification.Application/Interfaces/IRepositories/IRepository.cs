using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.Interfaces.IRepository
{
    public interface IRepository<T> where T : class 
    {
        Task<T?> GetAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetItemWhere(Expression<Func<T, bool>> predicate);
        IQueryable<T> AsQueryable(Expression<Func<T, bool>> predicate = null);
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        T Update(T entity);
        bool Remove(T entity);
        bool RemoveRange(IEnumerable<T> entities);
    }
}
