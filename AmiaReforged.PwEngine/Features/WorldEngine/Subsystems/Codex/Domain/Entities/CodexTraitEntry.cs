using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;

/// <summary>
/// Represents a single trait entry in a player's codex.
/// Trait metadata (name, description, category) is resolved from the trait subsystem
/// at the time the trait is acquired and stored here for offline display.
/// </summary>
public class CodexTraitEntry
{
    /// <summary>Unique trait identifier.</summary>
    public required TraitTag TraitTag { get; init; }

    /// <summary>Human-readable trait name.</summary>
    public required string Name { get; init; }

    /// <summary>Full description of the trait and its effects.</summary>
    public required string Description { get; init; }

    /// <summary>The broad category this trait belongs to.</summary>
    public required TraitCategory Category { get; init; }

    /// <summary>How the trait was acquired (e.g. "Character Creation", "DM Grant", "Quest Reward").</summary>
    public required string AcquisitionMethod { get; init; }

    /// <summary>When the trait was acquired.</summary>
    public DateTime DateAcquired { get; init; }

    /// <summary>
    /// Checks whether the entry matches a free-text search term.
    /// </summary>
    public bool MatchesSearch(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return false;

        return Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               Category.DisplayName().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               AcquisitionMethod.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks whether the entry belongs to the given category.
    /// </summary>
    public bool MatchesCategory(TraitCategory category) => Category == category;
}
