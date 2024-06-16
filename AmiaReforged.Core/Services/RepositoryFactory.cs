using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(IRepositoryFactory))]
public class RepositoryFactory : IRepositoryFactory
{
    private readonly DbContext _context;

    public RepositoryFactory(DatabaseContextFactory factory)
    {
        _context = factory.CreateDbContext();
    }

    public IRepository<T, TId> CreateRepository<T, TId>() where T : class
    {
        return new Repository<T, TId>(_context);
    }
}