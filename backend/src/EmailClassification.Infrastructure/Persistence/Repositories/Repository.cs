using EmailClassification.Application.Interfaces.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Infrastructure.Persistence.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected EmaildbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(EmaildbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }
        public async Task<T?> GetAsync(int id)
          => await _context.Set<T>().FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync()
            => await _context.Set<T>().ToListAsync();

        public async Task<T?> GetItemWhere(Expression<Func<T, bool>> predicate)
            => await _context.Set<T>().Where(predicate).FirstOrDefaultAsync();

        public IQueryable<T> AsQueryable(Expression<Func<T, bool>> predicate = null)
            => predicate == null ? _context.Set<T>().AsQueryable() : _context.Set<T>().Where(predicate).AsQueryable();

        public async Task<T> AddAsync(T entity)
        {
            try
            {
                var result = await _context.AddAsync(entity);
            }
            catch (System.Exception)
            {
                throw;
            }

            return entity;
        }

        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            try
            {
                await _context.AddRangeAsync(entities);
            }
            catch (System.Exception)
            {
                throw;
            }

            return entities;
        }

        public bool Remove(T entity)
        {
            try
            {
                var result = _context.Set<T>().Remove(entity);
                return true;
            }
            catch (System.Exception)
            {
                throw;
            }
        }


        public bool RemoveRange(IEnumerable<T> entities)
        {
            try
            {
                _context.Set<T>().RemoveRange(entities);
                return true;
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public T Update(T entity)
        {
            try
            {
                _context.Set<T>().Update(entity);
            }
            catch (System.Exception)
            {
                throw;
            }

            return entity;
        }

      
    }
}
