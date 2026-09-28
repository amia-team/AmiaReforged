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
    private readonly IDbContextFactory<PwEngineContext> _factory;

    public GlyphRepository(IDbContextFactory<PwEngineContext> factory)
    {
        _factory = factory;
    }

    // === Definitions ===

    public async Task<List<GlyphDefinition>> GetAllDefinitionsAsync()
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        return await _db.GlyphDefinitions
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<GlyphDefinition?> GetDefinitionByIdAsync(Guid id)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        return await _db.GlyphDefinitions
            .Include(d => d.Bindings)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task CreateDefinitionAsync(GlyphDefinition definition)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        _db.GlyphDefinitions.Add(definition);
        await _db.SaveChangesAsync();
        Log.Info("Created Glyph definition '{Name}' ({Id}).", definition.Name, definition.Id);
    }

    public async Task UpdateDefinitionAsync(GlyphDefinition definition)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        definition.UpdatedAt = DateTime.UtcNow;
        _db.GlyphDefinitions.Update(definition);
        await _db.SaveChangesAsync();
        Log.Info("Updated Glyph definition '{Name}' ({Id}).", definition.Name, definition.Id);
    }

    public async Task DeleteDefinitionAsync(Guid id)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        GlyphDefinition? definition = await _db.GlyphDefinitions.FindAsync(id);
        if (definition == null) return;

        _db.GlyphDefinitions.Remove(definition);
        await _db.SaveChangesAsync();
        Log.Info("Deleted Glyph definition '{Name}' ({Id}).", definition.Name, definition.Id);
    }

    // === Bindings ===

    public async Task<List<SpawnProfileGlyphBinding>> GetBindingsForProfileAsync(Guid profileId)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        return await _db.SpawnProfileGlyphBindings
            .Include(b => b.GlyphDefinition)
            .Where(b => b.SpawnProfileId == profileId)
            .OrderBy(b => b.Priority)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<SpawnProfileGlyphBinding>> GetAllBindingsAsync()
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        return await _db.SpawnProfileGlyphBindings
            .Include(b => b.GlyphDefinition)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<SpawnProfileGlyphBinding?> GetBindingByIdAsync(Guid id)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        return await _db.SpawnProfileGlyphBindings
            .Include(b => b.GlyphDefinition)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task CreateBindingAsync(SpawnProfileGlyphBinding binding)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        _db.SpawnProfileGlyphBindings.Add(binding);
        await _db.SaveChangesAsync();
        Log.Info("Created Glyph binding: profile={ProfileId}, definition={DefId}, priority={Priority}.",
            binding.SpawnProfileId, binding.GlyphDefinitionId, binding.Priority);
    }

    public async Task DeleteBindingAsync(Guid id)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        SpawnProfileGlyphBinding? binding = await _db.SpawnProfileGlyphBindings.FindAsync(id);
        if (binding == null) return;

        _db.SpawnProfileGlyphBindings.Remove(binding);
        await _db.SaveChangesAsync();
        Log.Info("Deleted Glyph binding {Id}.", id);
    }

    // === Trait Bindings ===

    public async Task<List<TraitGlyphBinding>> GetTraitBindingsForTagAsync(string traitTag)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        return await _db.TraitGlyphBindings
            .Include(b => b.GlyphDefinition)
            .Where(b => b.TraitTag == traitTag)
            .OrderBy(b => b.Priority)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<TraitGlyphBinding>> GetAllTraitBindingsAsync()
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        return await _db.TraitGlyphBindings
            .Include(b => b.GlyphDefinition)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task CreateTraitBindingAsync(TraitGlyphBinding binding)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        _db.TraitGlyphBindings.Add(binding);
        await _db.SaveChangesAsync();
        Log.Info("Created Glyph trait binding: tag={TraitTag}, definition={DefId}, priority={Priority}.",
            binding.TraitTag, binding.GlyphDefinitionId, binding.Priority);
    }

    public async Task DeleteTraitBindingAsync(Guid id)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        TraitGlyphBinding? binding = await _db.TraitGlyphBindings.FindAsync(id);
        if (binding == null) return;

        _db.TraitGlyphBindings.Remove(binding);
        await _db.SaveChangesAsync();
        Log.Info("Deleted Glyph trait binding {Id}.", id);
    }

    // === Definition-Scoped Bindings ===

    public async Task<List<SpawnProfileGlyphBinding>> GetSpawnBindingsForDefinitionAsync(Guid definitionId)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        return await _db.SpawnProfileGlyphBindings
            .Include(b => b.GlyphDefinition)
            .Where(b => b.GlyphDefinitionId == definitionId)
            .OrderBy(b => b.Priority)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<TraitGlyphBinding>> GetTraitBindingsForDefinitionAsync(Guid definitionId)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        return await _db.TraitGlyphBindings
            .Include(b => b.GlyphDefinition)
            .Where(b => b.GlyphDefinitionId == definitionId)
            .OrderBy(b => b.Priority)
            .AsNoTracking()
            .ToListAsync();
    }

    // === Interaction Bindings ===

    public async Task<List<InteractionGlyphBinding>> GetInteractionBindingsForTagAsync(string interactionTag)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        return await _db.InteractionGlyphBindings
            .Include(b => b.GlyphDefinition)
            .Where(b => b.InteractionTag == interactionTag)
            .OrderBy(b => b.Priority)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<InteractionGlyphBinding>> GetAllInteractionBindingsAsync()
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        return await _db.InteractionGlyphBindings
            .Include(b => b.GlyphDefinition)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task CreateInteractionBindingAsync(InteractionGlyphBinding binding)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        _db.InteractionGlyphBindings.Add(binding);
        await _db.SaveChangesAsync();
        Log.Info("Created Glyph interaction binding: tag={InteractionTag}, area={AreaResRef}, definition={DefId}, priority={Priority}.",
            binding.InteractionTag, binding.AreaResRef ?? "(global)", binding.GlyphDefinitionId, binding.Priority);
    }

    public async Task DeleteInteractionBindingAsync(Guid id)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        InteractionGlyphBinding? binding = await _db.InteractionGlyphBindings.FindAsync(id);
        if (binding == null) return;

        _db.InteractionGlyphBindings.Remove(binding);
        await _db.SaveChangesAsync();
        Log.Info("Deleted Glyph interaction binding {Id}.", id);
    }

    public async Task<List<InteractionGlyphBinding>> GetInteractionBindingsForDefinitionAsync(Guid definitionId)
    {
        await using PwEngineContext _db = await _factory.CreateDbContextAsync();
        return await _db.InteractionGlyphBindings
            .Include(b => b.GlyphDefinition)
            .Where(b => b.GlyphDefinitionId == definitionId)
            .OrderBy(b => b.Priority)
            .AsNoTracking()
            .ToListAsync();
    }
}
