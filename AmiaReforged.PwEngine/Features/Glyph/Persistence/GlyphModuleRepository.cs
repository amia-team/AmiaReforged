using AmiaReforged.PwEngine.Database;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

public interface IGlyphModuleRepository
{
    Task<List<GlyphModule>> GetAllAsync();
    Task<GlyphModule?> GetAsync(Guid id);
    Task SaveAsync(GlyphModule module, bool create = false);
}

[ServiceBinding(typeof(IGlyphModuleRepository))]
public sealed class GlyphModuleRepository(IDbContextFactory<PwEngineContext> factory) : IGlyphModuleRepository
{
    public async Task<List<GlyphModule>> GetAllAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.GlyphModules.AsNoTracking().OrderBy(m => m.Name).ToListAsync();
    }
    public async Task<GlyphModule?> GetAsync(Guid id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.GlyphModules.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
    }
    public async Task SaveAsync(GlyphModule module, bool create = false)
    {
        await using var db = await factory.CreateDbContextAsync();
        module.UpdatedAt = DateTime.UtcNow;
        if (create) db.GlyphModules.Add(module); else db.GlyphModules.Update(module);
        await db.SaveChangesAsync();
    }
}
