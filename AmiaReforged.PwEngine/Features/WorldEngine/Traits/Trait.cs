namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

/// <summary>
/// Aggregate root representing a trait definition.
/// Traits are background characteristics that provide bonuses, penalties, or special behaviors.
/// Loaded from JSON at startup and stored in memory via ITraitRepository.
/// </summary>
public class Trait
{
    /// <summary>
    /// Unique identifier for this trait (e.g., "brave", "coward", "hero")
    /// </summary>
    public required string Tag { get; init; }

    /// <summary>
    /// Display name shown to players
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description explaining what the trait does
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Point cost to select this trait. Can be negative for drawback traits that grant points.
    /// </summary>
    public required int PointCost { get; init; }

    /// <summary>
    /// If true, this trait must be unlocked via DM event or special achievement before selection.
    /// If false, trait is available to all characters at creation.
    /// </summary>
    public bool RequiresUnlock { get; init; }

    /// <summary>
    /// Defines how this trait behaves when the character dies.
    /// Default is Persist (trait remains unchanged).
    /// </summary>
    public TraitDeathBehavior DeathBehavior { get; init; } = TraitDeathBehavior.Persist;

    /// <summary>
    /// Mechanical effects this trait applies (skill bonuses, attribute mods, etc).
    /// Empty list means no mechanical effects (flavor trait only).
    /// </summary>
    public List<Effects.TraitEffect> Effects { get; init; } = [];

    /// <summary>
    /// If populated, only these races can select this trait.
    /// Empty list means no race restriction.
    /// </summary>
    public List<string> AllowedRaces { get; init; } = [];

    /// <summary>
    /// If populated, only these classes can select this trait.
    /// Empty list means no class restriction.
    /// </summary>
    public List<string> AllowedClasses { get; init; } = [];

    /// <summary>
    /// Races explicitly forbidden from selecting this trait, regardless of AllowedRaces.
    /// </summary>
    public List<string> ForbiddenRaces { get; init; } = [];

    /// <summary>
    /// Classes explicitly forbidden from selecting this trait, regardless of AllowedClasses.
    /// </summary>
    public List<string> ForbiddenClasses { get; init; } = [];

    /// <summary>
    /// Trait tags that cannot be selected alongside this trait (mutual exclusion).
    /// </summary>
    public List<string> ConflictingTraits { get; init; } = [];

    /// <summary>
    /// Trait tags that must be selected before this trait becomes available.
    /// </summary>
    public List<string> PrerequisiteTraits { get; init; } = [];
}
