namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

/// <summary>
/// Repository interface for Glyph definitions and profile bindings.
/// </summary>
public interface IGlyphRepository
{
    // === Definitions ===
    Task<List<GlyphDefinition>> GetAllDefinitionsAsync();
    Task<GlyphDefinition?> GetDefinitionByIdAsync(Guid id);
    Task CreateDefinitionAsync(GlyphDefinition definition);
    Task UpdateDefinitionAsync(GlyphDefinition definition);
    Task DeleteDefinitionAsync(Guid id);

    // === Bindings ===
    Task<List<SpawnProfileGlyphBinding>> GetBindingsForProfileAsync(Guid profileId);
    Task<List<SpawnProfileGlyphBinding>> GetAllBindingsAsync();
    Task<SpawnProfileGlyphBinding?> GetBindingByIdAsync(Guid id);
    Task CreateBindingAsync(SpawnProfileGlyphBinding binding);
    Task DeleteBindingAsync(Guid id);
}
