using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

/// <summary>
/// Entity representing a character's selected trait.
/// Tracks the relationship between a character and their chosen traits, including state and metadata.
/// </summary>
public class CharacterTrait
{
    /// <summary>
    /// Unique identifier for this character trait selection
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// ID of the character who selected this trait
    /// </summary>
    public required CharacterId CharacterId { get; init; }

    /// <summary>
    /// Tag referencing the trait definition (from Trait.Tag)
    /// </summary>
    public required TraitTag TraitTag { get; init; }

    /// <summary>
    /// When this trait was first selected by the character
    /// </summary>
    public DateTime DateAcquired { get; init; }

    /// <summary>
    /// Whether this trait has been confirmed during initial character creation.
    /// Confirmation prevents budget issues but does not permanently lock the trait.
    /// </summary>
    public bool IsConfirmed { get; set; }

    /// <summary>
    /// Whether this trait is currently active and applying its effects.
    /// Can be false if temporarily disabled (e.g., Hero trait after death).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this trait was unlocked via special achievement.
    /// Matches Trait.RequiresUnlock for tracking purposes.
    /// </summary>
    public bool IsUnlocked { get; init; }

    /// <summary>
    /// JSON or arbitrary string data for trait-specific state.
    /// Used by traits that need to track custom information (e.g., Hero bonuses, death counts).
    /// </summary>
    public string? CustomData { get; set; }
}
