using System.Linq.Expressions;

namespace AmiaReforged.Core.Services;

/// <summary>
/// Provides a generic interface for a repository. For an implementation, see <see cref="Repository{T,TId}"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TId"></typeparam>
public interface IRepository<T, in TId> where T : class
{
    /// <summary>
    /// Get an entity by its id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<T?> Get(TId id);
    
    /// <summary>
    /// Get all entities from the database.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<T?>> GetAll();
    
    /// <summary>
    /// Add an entity to the database.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task Add(T entity);
    
    /// <summary>
    /// Update a tracked entity. If the entity is not tracked, it will be tracked.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task Update(T entity);
    
    /// <summary>
    /// Delete an entity by its id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task Delete(TId id);

    /// <summary>
    /// Get all entities with the specified include properties.
    /// i.e: GetAllWith(x => x.Property1, x => x.Property2)
    /// </summary>
    /// <param name="includeProperties"></param>
    /// <returns></returns>
    Task<IEnumerable<T>> GetAllWith(params Expression<Func<T, object>>[] includeProperties);
    
    /// <summary>
    /// Get an entity with the specified include properties.
    /// i.e: GetWith(id, x => x.Property1, x => x.Property2)
    /// </summary>
    /// <param name="id"></param>
    /// <param name="includeProperties"></param>
    /// <returns></returns>
    Task<T?> GetWith(TId id, params Expression<Func<T, object>>[] includeProperties);
}