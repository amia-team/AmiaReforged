using AmiaReforged.Core.Models.Sailing.Ship.Weapon;
using AmiaReforged.Core.Models.Sailing.Ship.Weapon.Ammunition;

namespace AmiaReforged.Core.Models.Sailing.Ship.Armament;

public sealed class ShipArmament
{
    public required ShipArmamentSlot Slot { get; init; }

    public ShipWeaponType WeaponType { get; private set; }

    public bool IsOperational { get; private set; } = true;

    public Weapon.ShipWeapon Weapon => ShipWeaponCatalog.GetWeapon(WeaponType);

    public string DisplayName => Weapon.DisplayName;

    public WeaponArc Arc => Weapon.Arc;

    public float Range => Weapon.Range;

    public TimeSpan Cooldown => Weapon.Cooldown;

    public IReadOnlySet<ShipAmmunitionType> AcceptedAmmunition => Weapon.ValidAmmunition;

    public void ChangeWeapon(ShipWeaponType weaponType)
    {
        WeaponType = weaponType;
        IsOperational = true;
    }

    public void Disable()
    {
        IsOperational = false;
    }

    public void Repair()
    {
        IsOperational = true;
    }
}
