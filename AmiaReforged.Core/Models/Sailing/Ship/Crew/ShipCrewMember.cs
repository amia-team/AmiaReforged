using Anvil.API;

namespace AmiaReforged.Core.Models.Sailing.Ship.Crew;

public sealed class ShipCrewMember(NwCreature creature)
{
    public NwCreature Creature { get; } = creature;
    public ShipCrewRole Role { get; set; }
}
