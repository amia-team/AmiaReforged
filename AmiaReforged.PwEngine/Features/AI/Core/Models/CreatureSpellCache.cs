using Anvil.API;

namespace AmiaReforged.PwEngine.Features.AI.Core.Models;

/// <summary>
/// Cached spell list for a creature, organized by type and caster level.
/// Replaces the local variable spell storage from MakeSpellList() in ds_ai_include.nss (lines 857-942).
/// </summary>
public class CreatureSpellCache
{
    /// <summary>
    /// Cached spell metadata used by AI spell selection.
    /// </summary>
    public record AiSpellData(NwSpell Spell, int SpellPriority, SpellTalentType SpellTalent);

    /// <summary>
    /// All cached spells available to this creature.
    /// </summary>
    public IReadOnlyList<AiSpellData> Spells { get; init; } = [];

    /// <summary>
    /// Empty cache for disabled features or non-casters.
    /// </summary>
    public static CreatureSpellCache Empty { get; } = new();

    /// <summary>
    /// Tracks how many times each spell has been cast (spam prevention).
    /// SPAM_LIMIT = 2 from ds_ai_include.nss line 32.
    /// </summary>
    public Dictionary<NwSpell, int> SpellUsageCount { get; } = new();

    /// <summary>
    /// Checks if a spell has reached the spam limit (2 casts).
    /// </summary>
    public bool HasReachedSpamLimit(NwSpell spell) =>
        SpellUsageCount.GetValueOrDefault(spell) >= 2;
}
