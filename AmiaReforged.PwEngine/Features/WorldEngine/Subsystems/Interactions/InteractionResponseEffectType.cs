namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Categorizes the kind of runtime action that an interaction response effect triggers.
/// The actual execution of these effects is handled by a runtime subscriber listening to
/// <see cref="Events.InteractionResponseSelectedEvent"/>; the domain layer only describes intent.
/// </summary>
public enum InteractionResponseEffectType
{
    /// <summary>
    /// Display floating text on the character (Value = text content).
    /// Supports template placeholders like <c>{direction}</c>.
    /// </summary>
    FloatingText,

    /// <summary>
    /// Play a VFX at a world location (Value = VFX type name, e.g. <c>"ImpDustExplosion"</c>).
    /// Position comes from session metadata (<c>spawnX/Y/Z</c>) or trigger center.
    /// </summary>
    VfxAtLocation,

    /// <summary>
    /// Play a VFX on the player character (Value = VFX type name).
    /// </summary>
    VfxOnPlayer,

    /// <summary>
    /// Spawn a resource node nearby (Value = node definition tag or <c>"random"</c>).
    /// Uses area's <see cref="Regions.AreaDefinition.DefinitionTags"/> when <c>"random"</c>.
    /// </summary>
    SpawnResourceNode,

    /// <summary>
    /// Give the player a cardinal direction hint (Value = message template with <c>{direction}</c>).
    /// The runtime subscriber calculates the direction from the player to the nearest matching trigger/node.
    /// Example: <c>"You sense ore to the {direction}."</c>
    /// </summary>
    DirectionalHint,

    /// <summary>
    /// Custom effect handled by an extensible processor (Value = handler key).
    /// Metadata carries additional configuration.
    /// </summary>
    Custom
}
