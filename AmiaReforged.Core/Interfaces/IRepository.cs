namespace AmiaReforged.Core.Interfaces;

public interface IRepository<T> where T : class
{
    void Add(T entity);
}