namespace AmiaReforged.Core.Services;

public interface IRepository<T, in TId> where T : class
{
    Task<T?> Get(TId id);
    Task<IEnumerable<T?>> GetAll();
    Task Add(T entity);
    Task Update(T entity);
    Task Delete(TId id);
}