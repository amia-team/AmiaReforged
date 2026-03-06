using AmiaReforged.PwEngine.Database;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

/// <summary>
/// EF Core repository for Glyph definitions and profile bindings.
/// </summary>
[ServiceBinding(typeof(IGlyphRepository))]
public class GlyphRepository : IGlyphRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwEngineContext _db;

    public GlyphRepository(PwEngineContext db)
    {
        _db = db;
    }

    // === Definitions ===

    public async Task<List<GlyphDefinition>> GetAllDefinitionsAsync()
    {
        return await _db.GlyphDefinitions
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<GlyphDefinition?> GetDefinitionByIdAsync(Guid id)
    {
        return await _db.GlyphDefinitions
            .Include(d => d.Bindings)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task CreateDefinitionAsync(GlyphDefinition definition)
    {
        _db.GlyphDefinitions.Add(definition);
        await _db.SaveChangesAsync();
        Log.Info("Created Glyph definition '{Name}' ({Id}).", definition.Name, definition.Id);
    }

    public async Task UpdateDefinitionAsync(GlyphDefinition definition)
    {
        definition.UpdatedAt = DateTime.UtcNow;
        _db.GlyphDefinitions.Update(definition);
        await _db.SaveChangesAsync();
        Log.Info("Updated Glyph definition '{Name}' ({Id}).", definition.Name, definition.Id);
    }

    public async Task DeleteDefinitionAsync(Guid id)
    {
        GlyphDefinition? definition = await _db.GlyphDefinitions.FindAsync(id);
        if (definition == null) return;

        _db.GlyphDefinitions.Remove(definition);
        await _db.SaveChangesAsync();
        Log.Info("Deleted Glyph definition '{Name}' ({Id}).", definition.Name, definition.Id);
    }

    // === Bindings ===

    public async Task<List<SpawnProfileGlyphBinding>> GetBindingsForProfileAsync(Guid profileId)
    {
        return await _db.SpawnProfileGlyphBindings
            .Include(b => b.GlyphDefinition)
            .Where(b => b.SpawnProfileId == profileId)
            .OrderBy(b => b.Priority)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<SpawnProfileGlyphBinding>> GetAllBindingsAsync()
    {
        return await _db.SpawnProfileGlyphBindings
            .Include(b => b.GlyphDefinition)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<SpawnProfileGlyphBinding?> GetBindingByIdAsync(Guid id)
    {
        return await _db.SpawnProfileGlyphBindings
            .Include(b => b.GlyphDefinition)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task CreateBindingAsync(SpawnProfileGlyphBinding binding)
    {
        _db.SpawnProfileGlyphBindings.Add(binding);
        await _db.SaveChangesAsync();
        Log.Info("Created Glyph binding: profile={ProfileId}, definition={DefId}, priority={Priority}.",
            binding.SpawnProfileId, binding.GlyphDefinitionId, binding.Priority);
    }

    public async Task DeleteBindingAsync(Guid id)
    {
        SpawnProfileGlyphBinding? binding = await _db.SpawnProfileGlyphBindings.FindAsync(id);
        if (binding == null) return;

        _db.SpawnProfileGlyphBindings.Remove(binding);
        await _db.SaveChangesAsync();
        Log.Info("Deleted Glyph binding {Id}.", id);
    }
}
