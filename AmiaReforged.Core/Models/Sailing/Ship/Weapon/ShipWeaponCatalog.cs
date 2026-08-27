using AmiaReforged.Core.Models.Sailing.Ship.Types;

namespace AmiaReforged.Core.Models.Sailing.Ship.Weapon;

public static class ShipWeaponCatalog
{
    public static ShipWeapon GetWeapon(ShipWeaponType type)
    {
        ShipWeaponMap.TryGetValue(type, out ShipWeapon? weapon);
        return weapon ?? None;
    }

    private static readonly ShipWeapon Cannon = new
    (
        Type: ShipWeaponType.Cannon,
        ResRef: "ship_cannon",
        DisplayName: "Cannon",
        Damage: 10,
        Cooldown: TimeSpan.FromSeconds(3),
        Range: 10,
        Arc: WeaponArc.Broadside,
        ValidAmmunition: [ShipAmmunitionType.Cannonball, ShipAmmunitionType.FireCannonball]
    );

    private static readonly ShipWeapon Ballista = new
    (
        Type: ShipWeaponType.Ballista,
        ResRef: "ship_ballista",
        DisplayName: "Ballista",
        Damage: 7,
        Cooldown: TimeSpan.FromSeconds(2),
        Range: 12,
        Arc: WeaponArc.Forward,
        ValidAmmunition: [ShipAmmunitionType.BallistaBolt, ShipAmmunitionType.LightningBallistaBolt]
    );

    private static readonly ShipWeapon Catapult = new
    (
            Type: ShipWeaponType.Catapult,
            ResRef: "ship_catapult",
            DisplayName: "Catapult",
            Damage: 15,
            Cooldown: TimeSpan.FromSeconds(5),
            Range: 8,
            Arc: WeaponArc.Broadside,
            ValidAmmunition: [ShipAmmunitionType.CatapultStone, ShipAmmunitionType.ThornyCatapultStone]
    );

    private static readonly ShipWeapon HeavyCannon = new
    (
        Type: ShipWeaponType.HeavyCannon,
        ResRef: "ship_heavy_cannon",
        DisplayName: "Heavy Cannon",
        Damage: 20,
        Cooldown: TimeSpan.FromSeconds(6),
        Range: 9,
        Arc: WeaponArc.Broadside,
        ValidAmmunition: [ShipAmmunitionType.Cannonball, ShipAmmunitionType.HeavyCannonball, ShipAmmunitionType.FireCannonball]
    );

    private static readonly ShipWeapon None = new
    (
        Type: ShipWeaponType.None,
        ResRef: string.Empty,
        DisplayName: string.Empty,
        Damage: 0,
        Cooldown: TimeSpan.Zero,
        Range: 0,
        Arc: WeaponArc.Broadside,
        ValidAmmunition: []
    );

    private static readonly Dictionary<ShipWeaponType, ShipWeapon?> ShipWeaponMap = new()
    {
        { Cannon.Type, Cannon },
        { Ballista.Type, Ballista },
        { Catapult.Type, Catapult },
        { HeavyCannon.Type, HeavyCannon }
    };
}
