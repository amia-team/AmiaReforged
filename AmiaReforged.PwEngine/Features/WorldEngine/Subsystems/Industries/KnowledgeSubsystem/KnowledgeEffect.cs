namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// Categorizes the type of effect triggered when knowledge is learned.
/// </summary>
public enum KnowledgeEffectType
{
    /// <summary>
    /// Unlocks a recipe for the character (TargetTag = recipe ID).
    /// </summary>
    UnlockRecipe,

    /// <summary>
    /// Grants a codex lore entry to the character (TargetTag = lore definition tag).
    /// </summary>
    GrantCodexEntry,

    /// <summary>
    /// Modifies harvest behavior on resource nodes (TargetTag = node tag).
    /// Supersedes / supplements KnowledgeHarvestEffect for more flexible dispatch.
    /// </summary>
    ModifyHarvest,

    /// <summary>
    /// Custom effect handled by an extensible processor (TargetTag = handler key).
    /// </summary>
    Custom,

    /// <summary>
    /// Unlocks an interaction type for the character (TargetTag = interaction tag,
    /// e.g., <c>"prospecting"</c>). Processed by the Interaction Framework.
    /// </summary>
    UnlockInteraction
}

/// <summary>
/// Describes a side-effect triggered when a character learns a piece of Knowledge.
/// Acts as a bridge between the Knowledge subsystem and other subsystems
/// (Codex, Recipes, Harvesting, etc.).
/// </summary>
public class KnowledgeEffect
{
    /// <summary>
    /// What kind of effect this is.
    /// </summary>
    public required KnowledgeEffectType EffectType { get; init; }

    /// <summary>
    /// The target identifier — interpretation depends on <see cref="EffectType"/>:
    /// <list type="bullet">
    ///   <item><description>UnlockRecipe → recipe ID string</description></item>
    ///   <item><description>GrantCodexEntry → codex lore definition tag</description></item>
    ///   <item><description>ModifyHarvest → resource node tag</description></item>
    ///   <item><description>Custom → handler key for extensible dispatch</description></item>
    ///   <item><description>UnlockInteraction → interaction tag (e.g., "prospecting")</description></item>
    /// </list>
    /// </summary>
    public required string TargetTag { get; init; }

    /// <summary>
    /// Optional flexible metadata for effect-specific configuration.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
