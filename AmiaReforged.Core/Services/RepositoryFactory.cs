using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Core.Services;

/// <summary>
/// Abstracts away the need to register specific types of repositories in the DI container.
/// </summary>
[ServiceBinding(typeof(IRepositoryFactory))]
public class RepositoryFactory : IRepositoryFactory
{
    private readonly DbContext _context;

    /// <summary>
    /// Should not be typically instantiated directly. Use dependency injection to create this class instead.
    /// </summary>
    /// <param name="factory"></param>
    public RepositoryFactory(DatabaseContextFactory factory) => _context = factory.CreateDbContext();

    /// <summary>
    /// Provides a mechanism for creating a repository for a specific entity type. i.e: CreateRepository&lt;FactionEntity, string&gt;()
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <returns></returns>
    public IRepository<T, TId> CreateRepository<T, TId>() where T : class => new Repository<T, TId>(_context);
}