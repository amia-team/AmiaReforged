using Anvil.API;

namespace AmiaReforged.Classes.Associates.Bonuses;

/// <summary>
///     Implement this interface and mark with <c>[ServiceBinding(typeof(ICompanionBonusStrategy))]</c>
///     to register a resref-specific bonus strategy for animal companions.
///     Strategies are applied <b>after</b> the base companion bonuses.
/// </summary>
public interface ICompanionBonusStrategy
{
    /// <summary>
    ///     The resref prefix of the companion creature this strategy applies to.
    ///     The associate's full resref is matched via <c>StartsWith</c>, so level suffixes
    ///     (e.g. "01"–"40") are ignored. For example, "nw_ac_dire" matches "nw_ac_dire01" through "nw_ac_dire40".
    /// </summary>
    string ResRefPrefix { get; }

    /// <summary>
    ///     Apply custom bonuses or modifications to the companion.
    ///     Called after base companion bonuses have already been applied.
    /// </summary>
    void Apply(NwCreature owner, NwCreature associate);
}
