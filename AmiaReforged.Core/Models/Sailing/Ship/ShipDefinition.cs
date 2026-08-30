using AmiaReforged.Core.Models.Sailing.Ship.Types;

namespace AmiaReforged.Core.Models.Sailing.Ship;

public sealed record ShipDefinition
(
    int MaximumHull,
    int CargoCapacity,
    ShipArmamentDefinition[] Armaments
);

public sealed record ShipArmamentDefinition
(
    ShipArmamentSlot Slot,
    HashSet<ShipWeaponType> AllowedWeaponTypes,
    ShipWeaponType InitialWeaponType
);
