namespace AmiaReforged.PwEngine;

public interface IRepositoryBuilder
{
    IRepository<T, TId> Build<T, TId>() where T : class;
}