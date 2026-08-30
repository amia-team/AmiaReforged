namespace AmiaReforged.Core.Models.Sailing;

public sealed class ShipSpellEffectInstance
{
    /// <summary>
    /// Name of the spell that created this effect.
    /// </summary>
    public required string SpellName { get; init; }

    /// <summary>
    /// Movement multiplier applied while this effect is active.
    /// 1.0 means normal movement.
    /// 2.0 means double movement.
    /// </summary>
    public float SpeedMultiplier { get; init; } = 1.0f;

    /// <summary>
    /// Time at which the effect expires.
    /// </summary>
    public DateTime ExpiresAt { get; init; }
}