using AmiaReforged.Core.Models.Sailing.Ship.Types;

namespace AmiaReforged.Core.Models.Sailing.Ship;

public static class ShipTemplates
{
    public static ShipDefinition Sloop { get; } = new
    (
        MaximumHull: 100,
        CargoCapacity: 50,
        Armaments:
        [
            new ShipArmamentDefinition(
                Slot: ShipArmamentSlot.Forward,
                AllowedWeaponTypes: [ShipWeaponType.Ballista],
                InitialWeaponType: ShipWeaponType.Ballista)
        ]
    );

    public static ShipDefinition Brig { get; } = new
    (
        MaximumHull: 240,
        CargoCapacity: 100,
        Armaments:
        [
            new ShipArmamentDefinition
            (
                Slot: ShipArmamentSlot.Forward,
                AllowedWeaponTypes: [ShipWeaponType.Ballista],
                InitialWeaponType: ShipWeaponType.Ballista
            ),

            new ShipArmamentDefinition
            (
                Slot: ShipArmamentSlot.Port,
                AllowedWeaponTypes: [ShipWeaponType.Cannon],
                InitialWeaponType: ShipWeaponType.Cannon
            ),

            new ShipArmamentDefinition
            (
                Slot: ShipArmamentSlot.Starboard,
                AllowedWeaponTypes: [ShipWeaponType.Cannon],
                InitialWeaponType: ShipWeaponType.Cannon
            )
        ]
    );

    public static ShipDefinition Galleon { get; } = new
    (
        MaximumHull: 640,
        CargoCapacity: 180,
        Armaments:
        [
            new ShipArmamentDefinition
            (
                Slot: ShipArmamentSlot.Forward,
                AllowedWeaponTypes: [ShipWeaponType.Ballista, ShipWeaponType.Catapult],
                InitialWeaponType: ShipWeaponType.Ballista
            ),

            new ShipArmamentDefinition
            (
                Slot: ShipArmamentSlot.Aft,
                AllowedWeaponTypes: [ShipWeaponType.Ballista],
                InitialWeaponType: ShipWeaponType.Ballista
            ),

            new ShipArmamentDefinition
            (
                Slot: ShipArmamentSlot.PortForward,
                AllowedWeaponTypes:
                [ShipWeaponType.Cannon, ShipWeaponType.HeavyCannon],
                InitialWeaponType: ShipWeaponType.HeavyCannon
            ),

            new ShipArmamentDefinition
            (
                Slot: ShipArmamentSlot.PortAft,
                AllowedWeaponTypes: [ShipWeaponType.Cannon, ShipWeaponType.HeavyCannon],
                InitialWeaponType: ShipWeaponType.HeavyCannon
            ),

            new ShipArmamentDefinition
            (
                Slot: ShipArmamentSlot.StarboardForward,
                AllowedWeaponTypes: [ShipWeaponType.Cannon, ShipWeaponType.HeavyCannon],
                InitialWeaponType: ShipWeaponType.HeavyCannon
            ),

            new ShipArmamentDefinition
            (
                Slot: ShipArmamentSlot.StarboardAft,
                AllowedWeaponTypes: [ShipWeaponType.Cannon, ShipWeaponType.HeavyCannon],
                InitialWeaponType: ShipWeaponType.HeavyCannon
            )
        ]
    );
}
