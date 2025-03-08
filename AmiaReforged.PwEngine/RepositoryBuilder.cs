using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine;

/// <summary>
///     Abstracts away the need to register specific types of repositories in the DI container.
/// </summary>
public class RepositoryBuilder : IRepositoryBuilder, IContextSelectionStage
{
    private DbContext _context;

    private RepositoryBuilder()
    {
    }

    public IRepositoryBuilder WithContext(DbContext context)
    {
        _context = context;
        return this;
    }

    /// <summary>
    ///     Provides a mechanism for creating a repository for a specific entity type. i.e: CreateRepository&lt;FactionEntity,
    ///     string&gt;()
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <returns></returns>
    public IRepository<T, TId> Build<T, TId>() where T : class => new Repository<T, TId>(_context);

    public static IContextSelectionStage Make() => new RepositoryBuilder();
}

public interface IContextSelectionStage
{
    IRepositoryBuilder WithContext(DbContext context);
}