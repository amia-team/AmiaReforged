using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Core.Services;

/// <summary>
/// Implements a generic repository for database operations. For dependency injection purposes, use <see cref="RepositoryFactory"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TId"></typeparam>
public class Repository<T, TId> : IRepository<T, TId> where T : class
{
    private readonly DbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<T?> Get(TId id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T?>> GetAll()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task Add(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task Update(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(TId id)
    {
        T? entity = await Get(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<T>> GetAllWith(params Expression<Func<T, object>>[] includeProperties)
    {
        IQueryable<T> query = includeProperties.Aggregate<Expression<Func<T, object>>, IQueryable<T>>(_dbSet,
            (current, includeProperty) => current.Include(includeProperty));

        return await query.ToListAsync();
    }

    public async Task<T?> GetWith(TId id, params Expression<Func<T, object>>[] includeProperties)
    {
        IQueryable<T> query = includeProperties.Aggregate<Expression<Func<T, object>>, IQueryable<T>>(_dbSet,
            (current, includeProperty) => current.Include(includeProperty));

        return await query.FirstOrDefaultAsync();
    }
}