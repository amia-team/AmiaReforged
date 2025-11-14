using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

/// <summary>
/// Provides access to trait-related operations including character traits and trait effects.
/// </summary>
public interface ITraitSubsystem
{
    // === Trait Management ===

    /// <summary>
    /// Gets a trait definition by tag.
    /// </summary>
    Task<TraitDefinition?> GetTraitAsync(TraitTag traitTag, CancellationToken ct = default);

    /// <summary>
    /// Gets all available trait definitions.
    /// </summary>
    Task<List<TraitDefinition>> GetAllTraitsAsync(CancellationToken ct = default);

    // === Character Traits ===

    /// <summary>
    /// Grants a trait to a character.
    /// </summary>
    Task<CommandResult> GrantTraitAsync(
        CharacterId characterId,
        TraitTag traitTag,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a trait from a character.
    /// </summary>
    Task<CommandResult> RemoveTraitAsync(
        CharacterId characterId,
        TraitTag traitTag,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all traits possessed by a character.
    /// </summary>
    Task<List<CharacterTrait>> GetCharacterTraitsAsync(
        CharacterId characterId,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a character has a specific trait.
    /// </summary>
    Task<bool> HasTraitAsync(
        CharacterId characterId,
        TraitTag traitTag,
        CancellationToken ct = default);

    // === Trait Effects ===

    /// <summary>
    /// Applies trait effects to a character (calculated based on their traits).
    /// </summary>
    Task<TraitEffectsSummary> CalculateTraitEffectsAsync(
        CharacterId characterId,
        CancellationToken ct = default);
}

/// <summary>
/// Represents a trait definition.
/// </summary>
public record TraitDefinition(
    TraitTag Tag,
    string Name,
    string Description,
    TraitCategory Category,
    Dictionary<string, object> Effects);

/// <summary>
/// Represents a trait possessed by a character.
/// </summary>
public record CharacterTrait(
    TraitTag TraitTag,
    string Name,
    DateTime GrantedAt,
    string? GrantedBy);

/// <summary>
/// Summary of all trait effects for a character.
/// </summary>
public record TraitEffectsSummary(
    CharacterId CharacterId,
    Dictionary<string, int> StatModifiers,
    List<string> SpecialAbilities,
    List<string> Restrictions);

/// <summary>
/// Categories of traits.
/// </summary>
public enum TraitCategory
{
    Background,
    Personality,
    Physical,
    Mental,
    Social,
    Supernatural,
    Curse,
    Blessing
}
