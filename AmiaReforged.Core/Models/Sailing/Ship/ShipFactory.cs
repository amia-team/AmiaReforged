using AmiaReforged.Core.Models.Sailing.Ship.Armament;
using AmiaReforged.Core.Models.Sailing.Ship.Types;

namespace AmiaReforged.Core.Models.Sailing.Ship;

public static class ShipFactory
{
    public static Ship Create(
        string name,
        ShipFaction type,
        ShipDefinition definition,
        ShipPosition position,
        Heading heading = Heading.East)
    {
        ShipArmament[] armaments = definition.Armaments
            .Select(CreateArmament)
            .ToArray();

        return new Ship(
            name: name,
            faction: type,
            maximumHull: definition.MaximumHull,
            position: position,
            heading: heading,
            armaments: armaments,
            cargoCapacity: definition.CargoCapacity);
    }

    private static ShipArmament CreateArmament(ShipArmamentDefinition definition) =>
        new(slot: definition.Slot, allowedWeaponTypes: definition.AllowedWeaponTypes, initialWeaponType: definition.InitialWeaponType);
}
