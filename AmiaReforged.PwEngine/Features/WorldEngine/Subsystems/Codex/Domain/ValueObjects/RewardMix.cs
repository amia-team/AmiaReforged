namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;

/// <summary>
/// Immutable value object describing a bundle of rewards granted upon completing
/// a quest stage or the quest itself. Supports XP, gold, knowledge points,
/// and per-industry proficiency grants.
/// </summary>
public sealed record RewardMix
{
    /// <summary>Experience points awarded.</summary>
    public int Xp { get; init; }

    /// <summary>Gold pieces awarded.</summary>
    public int Gold { get; init; }

    /// <summary>Knowledge points awarded (used in the WorldEngine progression system).</summary>
    public int KnowledgePoints { get; init; }

    /// <summary>
    /// Per-industry proficiency XP grants (e.g., +50 proficiency XP in Alchemy).
    /// </summary>
    public List<ProficiencyReward> Proficiencies { get; init; } = [];

    /// <summary>A reward mix with no rewards.</summary>
    public static RewardMix Empty => new();

    /// <summary>True when every field is at its default (no rewards).</summary>
    public bool IsEmpty => Xp == 0 && Gold == 0 && KnowledgePoints == 0 && Proficiencies.Count == 0;

    /// <summary>Sequence-aware equality for <see cref="Proficiencies"/>.</summary>
    public bool Equals(RewardMix? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Xp == other.Xp
               && Gold == other.Gold
               && KnowledgePoints == other.KnowledgePoints
               && Proficiencies.SequenceEqual(other.Proficiencies);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hc = new();
        hc.Add(Xp);
        hc.Add(Gold);
        hc.Add(KnowledgePoints);
        foreach (ProficiencyReward p in Proficiencies) hc.Add(p);
        return hc.ToHashCode();
    }

    /// <summary>
    /// Validates that all reward amounts are non-negative and proficiency entries are well-formed.
    /// Returns null if valid, otherwise a human-readable error message.
    /// </summary>
    public string? Validate()
    {
        if (Xp < 0) return "XP reward cannot be negative";
        if (Gold < 0) return "Gold reward cannot be negative";
        if (KnowledgePoints < 0) return "Knowledge points reward cannot be negative";

        foreach (ProficiencyReward prof in Proficiencies)
        {
            if (string.IsNullOrWhiteSpace(prof.IndustryTag))
                return "Proficiency reward must specify an industry tag";
            if (prof.ProficiencyXp < 0)
                return $"Proficiency XP for '{prof.IndustryTag}' cannot be negative";
        }

        return null;
    }
}

/// <summary>
/// A single proficiency XP grant targeting a specific industry.
/// </summary>
public sealed record ProficiencyReward
{
    /// <summary>Tag of the industry (e.g., "alchemy", "smithing").</summary>
    public required string IndustryTag { get; init; }

    /// <summary>Amount of proficiency XP to award in that industry.</summary>
    public int ProficiencyXp { get; init; }
}
