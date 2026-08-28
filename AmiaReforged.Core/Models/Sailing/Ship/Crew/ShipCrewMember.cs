using Anvil.API;

namespace AmiaReforged.Core.Models.Sailing.Ship.Crew;

/// <summary>
/// Represents an individual creature assigned to a ship's crew.
/// </summary>
/// <param name="creature">The underlying NWN creature.</param>
public sealed class ShipCrewMember(NwCreature creature)
{
    /// <summary>
    /// The NWN creature associated with this crew member.
    /// </summary>
    public NwCreature Creature { get; } = creature;

    /// <summary>
    /// The specific duty or rank assigned to this crew member.
    /// </summary>
    public ShipCrewRole Role { get; set; }
}
