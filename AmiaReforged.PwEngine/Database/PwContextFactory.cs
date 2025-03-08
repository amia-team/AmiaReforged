using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(PwContextFactory))]
public class PwContextFactory : IDbContextFactory<PwEngineContext>
{
    public PwEngineContext CreateDbContext() => new();
}