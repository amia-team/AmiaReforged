using Anvil.API;

namespace AmiaReforged.Classes.Associates.Bonuses;

/// <summary>
///     Implement this interface and mark with <c>[ServiceBinding(typeof(IFamiliarBonusStrategy))]</c>
///     to register a resref-specific bonus strategy for familiars.
///     Strategies are applied <b>after</b> the base familiar bonuses.
/// </summary>
public interface IFamiliarBonusStrategy
{
    /// <summary>
    ///     The resref prefix of the familiar creature this strategy applies to.
    ///     The associate's full resref is matched via <c>StartsWith</c>, so level suffixes
    ///     (e.g. "01"–"40") are ignored. For example, "nw_fm_rave" matches "nw_fm_rave01" through "nw_fm_rave40".
    /// </summary>
    string ResRefPrefix { get; }

    /// <summary>
    ///     Apply custom bonuses or modifications to the familiar.
    ///     Called after base familiar bonuses have already been applied.
    /// </summary>
    void Apply(NwCreature owner, NwCreature associate);
}
