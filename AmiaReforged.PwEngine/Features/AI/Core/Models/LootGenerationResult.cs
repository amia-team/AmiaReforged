using Anvil.API;

namespace AmiaReforged.PwEngine.Features.AI.Core.Models;

/// <summary>
/// Result of loot generation for a killed creature.
/// </summary>
public sealed class LootGenerationResult
{
    /// <summary>
    /// Whether any loot was generated.
    /// </summary>
    public bool LootGenerated { get; init; }

    /// <summary>
    /// The loot bag placeable that was created (if any).
    /// </summary>
    public NwPlaceable? LootBag { get; init; }

    /// <summary>
    /// Items that were created.
    /// </summary>
    public IReadOnlyList<NwItem> GeneratedItems { get; init; } = Array.Empty<NwItem>();

    /// <summary>
    /// The loot tier that was used.
    /// </summary>
    public LootTier Tier { get; init; }

    /// <summary>
    /// Whether a mythal crystal was dropped.
    /// </summary>
    public bool DroppedMythal { get; init; }

    /// <summary>
    /// Whether a special item was dropped (bone wand, parchment, deity ring).
    /// </summary>
    public bool DroppedSpecialItem { get; init; }

    /// <summary>
    /// Creates an empty result (no loot generated).
    /// </summary>
    public static LootGenerationResult None() => new()
    {
        LootGenerated = false
    };
}
