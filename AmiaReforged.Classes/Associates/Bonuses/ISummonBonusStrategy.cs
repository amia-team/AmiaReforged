using Anvil.API;

namespace AmiaReforged.Classes.Associates.Bonuses;

/// <summary>
///     Implement this interface and mark with <c>[ServiceBinding(typeof(ISummonBonusStrategy))]</c>
///     to register a resref-specific bonus strategy for summoned creatures.
///     Strategies are applied <b>after</b> the base summon bonuses.
/// </summary>
public interface ISummonBonusStrategy
{
    /// <summary>
    ///     The resref prefix of the summoned creature this strategy applies to.
    ///     The associate's full resref is matched via <c>StartsWith</c>, so level suffixes
    ///     are ignored. Use the full resref for an exact match.
    /// </summary>
    string ResRefPrefix { get; }

    /// <summary>
    ///     Apply custom bonuses or modifications to the summoned creature.
    ///     Called after base summon bonuses have already been applied.
    /// </summary>
    void Apply(NwCreature owner, NwCreature associate);
}
