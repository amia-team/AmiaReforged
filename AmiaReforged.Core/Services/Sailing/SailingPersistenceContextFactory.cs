using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Uses the existing database configuration without modifying its context or opening a connection.</summary>
[ServiceBinding(typeof(SailingPersistenceContextFactory))]
public sealed class SailingPersistenceContextFactory : IDbContextFactory<SailingPersistenceDbContext>
{
    private readonly DatabaseContextFactory? _applicationFactory;
    private readonly DbContextOptions<SailingPersistenceDbContext>? _options;

    public SailingPersistenceContextFactory(DatabaseContextFactory applicationFactory)
        => _applicationFactory = applicationFactory;

    private SailingPersistenceContextFactory(DbContextOptions<SailingPersistenceDbContext> options)
        => _options = options;

    /// <summary>Allows an isolated database to be supplied without reading server configuration.</summary>
    public static SailingPersistenceContextFactory ForOptions(DbContextOptions<SailingPersistenceDbContext> options)
        => new(options);

    public SailingPersistenceDbContext CreateDbContext()
    {
        if (_options != null) return new SailingPersistenceDbContext(_options);
        using AmiaDbContext applicationContext = _applicationFactory!.CreateDbContext();
        string connectionString = applicationContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("The application database connection is not configured.");
        DbContextOptions<SailingPersistenceDbContext> options =
            new DbContextOptionsBuilder<SailingPersistenceDbContext>().UseNpgsql(connectionString).Options;
        return new SailingPersistenceDbContext(options);
    }
}
