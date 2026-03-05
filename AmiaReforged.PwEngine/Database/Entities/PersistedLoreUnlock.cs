using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Database.Entities;

/// <summary>
/// EF Core join entity recording that a specific player has unlocked a lore entry.
/// Per-player discovery metadata (location, source) lives here, while the lore
/// content itself lives in <see cref="PersistedLoreDefinition"/>.
/// Maps to the <c>codex_lore_unlocks</c> table in the PwEngine database.
/// </summary>
public class PersistedLoreUnlock
{
    /// <summary>
    /// The character who unlocked this lore (FK to persisted_characters).
    /// Part of composite primary key.
    /// </summary>
    public Guid CharacterId { get; set; }

    /// <summary>
    /// The lore entry that was unlocked (FK to codex_lore_definitions).
    /// Part of composite primary key.
    /// </summary>
    [MaxLength(100)]
    public required string LoreId { get; set; }

    /// <summary>
    /// UTC timestamp when this player discovered the lore.
    /// </summary>
    public DateTime DateDiscovered { get; set; }

    /// <summary>
    /// Optional location where this specific player discovered the lore.
    /// </summary>
    [MaxLength(200)]
    public string? DiscoveryLocation { get; set; }

    /// <summary>
    /// Optional source or method of discovery (e.g. "Ancient Scroll", "NPC Conversation").
    /// </summary>
    [MaxLength(200)]
    public string? DiscoverySource { get; set; }

    /// <summary>
    /// Navigation property to the global lore definition.
    /// </summary>
    public PersistedLoreDefinition? LoreDefinition { get; set; }
}
