namespace AmiaReforged.Core.Models.Sailing;

public sealed class ShipSpellEffectDefinition
{
    /// <summary>
    /// The NWN spell ID this sailing effect represents.
    /// </summary>
    public int SpellId { get; init; }

    /// <summary>
    /// Display name used by the sailing system.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Determines how the spell interacts with a ship.
    /// </summary>
    public ShipSpellEffectType EffectType { get; init; }

    /// <summary>
    /// Amount of hull damage dealt by the spell.
    /// </summary>
    public int HullDamage { get; init; }

    /// <summary>
    /// Movement multiplier applied while the effect is active.
    /// 1.0 means normal movement.
    /// 2.0 means double movement.
    /// </summary>
    public float SpeedMultiplier { get; init; } = 1.0f;

    /// <summary>
    /// Duration of a temporary sailing effect.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Whether this spell requires an enemy ship as its target.
    /// </summary>
    public bool RequiresEnemyTarget { get; init; }

    /// <summary>
    /// Whether the effect can be used while the ship is in an encounter.
    /// </summary>
    public bool RequiresEncounter { get; init; }
}