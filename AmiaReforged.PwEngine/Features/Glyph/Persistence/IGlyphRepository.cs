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

    // === Trait Bindings ===
    Task<List<TraitGlyphBinding>> GetTraitBindingsForTagAsync(string traitTag);
    Task<List<TraitGlyphBinding>> GetAllTraitBindingsAsync();
    Task CreateTraitBindingAsync(TraitGlyphBinding binding);
    Task DeleteTraitBindingAsync(Guid id);

    // === Definition-Scoped Bindings ===
    Task<List<SpawnProfileGlyphBinding>> GetSpawnBindingsForDefinitionAsync(Guid definitionId);
    Task<List<TraitGlyphBinding>> GetTraitBindingsForDefinitionAsync(Guid definitionId);
    Task<List<InteractionGlyphBinding>> GetInteractionBindingsForDefinitionAsync(Guid definitionId);

    // === Interaction Bindings ===
    Task<List<InteractionGlyphBinding>> GetInteractionBindingsForTagAsync(string interactionTag);
    Task<List<InteractionGlyphBinding>> GetAllInteractionBindingsAsync();
    Task CreateInteractionBindingAsync(InteractionGlyphBinding binding);
    Task DeleteInteractionBindingAsync(Guid id);
}
