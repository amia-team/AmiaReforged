namespace AmiaReforged.Core.Services;

public interface IRepositoryFactory
{
    IRepository<T, TId> CreateRepository<T, TId>() where T : class;
}