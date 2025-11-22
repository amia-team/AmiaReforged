using Anvil.API;

namespace AmiaReforged.PwEngine.Features.AI.Core.Models;

/// <summary>
/// Cached spell list for a creature, organized by type and caster level.
/// Replaces the local variable spell storage from MakeSpellList() in ds_ai_include.nss (lines 857-942).
/// </summary>
public class CreatureSpellCache
{
    /// <summary>
    /// Maximum caster level detected for this creature (0-10 range).
    /// </summary>
    public int MaxCasterLevel { get; init; }

    /// <summary>
    /// Spells organized by caster level (0-10).
    /// Used for spell priority selection.
    /// </summary>
    public IReadOnlyDictionary<int, List<Spell>> SpellsByCasterLevel { get; init; } =
        new Dictionary<int, List<Spell>>();

    /// <summary>
    /// Attack/offensive spells (categories 1, 2, 3, 11, 22, 19 from categories.2da).
    /// </summary>
    public IReadOnlyList<Spell> AttackSpells { get; init; } = Array.Empty<Spell>();

    /// <summary>
    /// Buff/beneficial spells (categories 6-14, 18, 20, 21 from categories.2da).
    /// </summary>
    public IReadOnlyList<Spell> BuffSpells { get; init; } = Array.Empty<Spell>();

    /// <summary>
    /// Healing spells (categories 4, 5, 17 from categories.2da).
    /// </summary>
    public IReadOnlyList<Spell> HealingSpells { get; init; } = Array.Empty<Spell>();

    /// <summary>
    /// Summoning spells (category 15 from categories.2da).
    /// </summary>
    public IReadOnlyList<Spell> SummonSpells { get; init; } = Array.Empty<Spell>();

    /// <summary>
    /// Dispel/counter spells (category 23 from categories.2da).
    /// </summary>
    public IReadOnlyList<Spell> DispelSpells { get; init; } = Array.Empty<Spell>();

    /// <summary>
    /// Persistent area of effect spells (category 16 from categories.2da).
    /// </summary>
    public IReadOnlyList<Spell> PersistentAoeSpells { get; init; } = Array.Empty<Spell>();

    /// <summary>
    /// Tracks how many times each spell has been cast (spam prevention).
    /// SPAM_LIMIT = 2 from ds_ai_include.nss line 32.
    /// </summary>
    public Dictionary<Spell, int> SpellUsageCount { get; } = new();

    /// <summary>
    /// Checks if a spell has reached the spam limit (2 casts).
    /// </summary>
    public bool HasReachedSpamLimit(Spell spell) =>
        SpellUsageCount.GetValueOrDefault(spell) >= 2;

    /// <summary>
    /// Empty cache for disabled features or non-casters.
    /// </summary>
    public static readonly CreatureSpellCache Empty = new()
    {
        MaxCasterLevel = 0,
        SpellsByCasterLevel = new Dictionary<int, List<Spell>>(),
        AttackSpells = Array.Empty<Spell>(),
        BuffSpells = Array.Empty<Spell>(),
        HealingSpells = Array.Empty<Spell>(),
        SummonSpells = Array.Empty<Spell>(),
        DispelSpells = Array.Empty<Spell>(),
        PersistentAoeSpells = Array.Empty<Spell>()
    };
}

