using System.ComponentModel.DataAnnotations;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;

namespace AmiaReforged.PwEngine.Database.Entities;

/// <summary>
/// EF Core entity for a global lore definition.
/// Shared across all players — individual players "unlock" access to these entries.
/// Maps to the <c>codex_lore_definitions</c> table in the PwEngine database.
/// </summary>
public class PersistedLoreDefinition
{
    /// <summary>
    /// Unique lore identifier (natural key from <c>LoreId</c> value object).
    /// </summary>
    [Key]
    [MaxLength(100)]
    public required string LoreId { get; set; }

    /// <summary>
    /// Display title of the lore entry.
    /// </summary>
    [MaxLength(200)]
    public required string Title { get; set; }

    /// <summary>
    /// Full lore content / body text.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Knowledge-skill category. Stored in the database as the enum name string.
    /// </summary>
    public LoreCategory Category { get; set; }

    /// <summary>
    /// Integer value of <c>LoreTier</c> enum (Common = 0, Uncommon = 1, Rare = 2, Legendary = 3).
    /// </summary>
    public int Tier { get; set; }

    /// <summary>
    /// Comma-separated lowercase keywords for searching / filtering.
    /// </summary>
    [MaxLength(1000)]
    public string? Keywords { get; set; }

    /// <summary>
    /// When <c>true</c>, this lore entry is visible to every player without
    /// requiring an unlock record (e.g. server rules, OOC information).
    /// </summary>
    public bool IsAlwaysAvailable { get; set; }

    /// <summary>
    /// UTC timestamp when the definition was first persisted.
    /// </summary>
    public DateTime CreatedUtc { get; set; }
}
