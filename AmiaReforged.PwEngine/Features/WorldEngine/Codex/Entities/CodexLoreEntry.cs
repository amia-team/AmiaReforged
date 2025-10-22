using AmiaReforged.PwEngine.Features.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Codex.Entities;

/// <summary>
/// Entity representing a lore entry in a player's codex.
/// Immutable once discovered, tracks lore knowledge and categorization.
/// </summary>
public class CodexLoreEntry
{
    /// <summary>
    /// Unique identifier for this lore entry
    /// </summary>
    public required LoreId LoreId { get; init; }

    /// <summary>
    /// Display title of the lore entry
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Full lore content/text
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Category or source of the lore (e.g., "History", "Geography", "Religion")
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Rarity/importance tier of this lore
    /// </summary>
    public required LoreTier Tier { get; init; }

    /// <summary>
    /// When this lore was discovered
    /// </summary>
    public DateTime DateDiscovered { get; init; }

    /// <summary>
    /// Optional location where lore was discovered
    /// </summary>
    public string? DiscoveryLocation { get; init; }

    /// <summary>
    /// Optional source or method of discovery (e.g., "Ancient Scroll", "NPC Conversation")
    /// </summary>
    public string? DiscoverySource { get; init; }

    /// <summary>
    /// Keywords for searching and filtering
    /// </summary>
    public List<Keyword> Keywords { get; init; } = new();

    /// <summary>
    /// Checks if the lore entry matches any of the provided search terms
    /// </summary>
    public bool MatchesSearch(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return false;

        string lowerSearch = searchTerm.ToLowerInvariant();

        return Title.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
               Content.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
               Category.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
               (DiscoveryLocation?.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (DiscoverySource?.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
               Keywords.Any(k => k.Matches(searchTerm));
    }

    /// <summary>
    /// Checks if the lore entry matches the specified tier
    /// </summary>
    public bool MatchesTier(LoreTier tier) => Tier == tier;

    /// <summary>
    /// Checks if the lore entry belongs to the specified category
    /// </summary>
    public bool MatchesCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        return Category.Equals(category, StringComparison.OrdinalIgnoreCase);
    }
}
