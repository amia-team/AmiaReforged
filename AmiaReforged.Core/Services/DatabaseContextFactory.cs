using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(DatabaseContextFactory))]
public class DatabaseContextFactory : IDbContextFactory<AmiaDbContext>
{
    public AmiaDbContext CreateDbContext()
    {
        return new();
    }
}