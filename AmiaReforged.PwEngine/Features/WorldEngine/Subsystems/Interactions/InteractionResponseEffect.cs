namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Describes a single effect that fires when an <see cref="InteractionResponse"/> is selected
/// after a data-driven interaction completes. The domain layer captures the intent;
/// a runtime subscriber interprets the <see cref="EffectType"/> and <see cref="Value"/>
/// to apply VFX, messages, spawns, etc.
/// </summary>
public class InteractionResponseEffect
{
    /// <summary>
    /// What kind of runtime action this effect represents.
    /// </summary>
    public required InteractionResponseEffectType EffectType { get; init; }

    /// <summary>
    /// Primary payload whose interpretation depends on <see cref="EffectType"/>:
    /// <list type="bullet">
    ///   <item><description><see cref="InteractionResponseEffectType.FloatingText"/> → text content</description></item>
    ///   <item><description><see cref="InteractionResponseEffectType.VfxAtLocation"/> → VFX type name</description></item>
    ///   <item><description><see cref="InteractionResponseEffectType.VfxOnPlayer"/> → VFX type name</description></item>
    ///   <item><description><see cref="InteractionResponseEffectType.SpawnResourceNode"/> → node definition tag or <c>"random"</c></description></item>
    ///   <item><description><see cref="InteractionResponseEffectType.DirectionalHint"/> → message template with <c>{direction}</c></description></item>
    ///   <item><description><see cref="InteractionResponseEffectType.Custom"/> → handler key</description></item>
    /// </list>
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Optional effect-specific configuration (e.g., VFX scale, offset, duration).
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
