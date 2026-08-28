using AmiaReforged.Core.Models.Sailing.Ship.Types;
using AmiaReforged.Core.Models.Sailing.Ship.Weapon;

namespace AmiaReforged.Core.Models.Sailing.Ship.Armament;

/// <summary>
/// Represents a specific weapon system installed in a designated slot on a ship.
/// It wraps a <see cref="ShipWeapon"/> to manage its state (e.g., operational status, cooldowns)
/// on a per-ship basis.
/// </summary>
public sealed class ShipArmament
{
    /// <summary>
    /// The physical location on the ship where this weapon is mounted (e.g., Bow, Port).
    /// </summary>
    public required ShipArmamentSlot Slot { get; init; }

    /// <summary>
    /// The specific model or type of weapon installed in this slot.
    /// </summary>
    public ShipWeaponType WeaponType { get; private set; }

    /// <summary>
    /// Indicates whether this weapon is currently functional. A weapon might become non-operational
    /// due to damage or special effects.
    /// </summary>
    public bool IsOperational { get; private set; } = true;

    /// <summary>
    /// The underlying weapon template containing static data like base range and damage.
    /// </summary>
    public Weapon.ShipWeapon Weapon => ShipWeaponCatalog.GetWeapon(WeaponType);

    /// <summary>
    /// The user-friendly name of the installed weapon.
    /// </summary>
    public string DisplayName => Weapon.DisplayName;

    /// <summary>
    /// The firing arc coverage of the weapon.
    /// </summary>
    public WeaponArc Arc => Weapon.Arc;

    /// <summary>
    /// The maximum distance this weapon can fire, as defined by its template.
    /// </summary>
    public float Range => Weapon.Range;

    /// <summary>
    /// The minimum time that must pass between successive shots.
    /// </summary>
    public TimeSpan Cooldown => Weapon.Cooldown;

    /// <summary>
    /// A set of ammunition types that this weapon is capable of firing.
    /// </summary>
    public IReadOnlySet<ShipAmmunitionType> AcceptedAmmunition => Weapon.ValidAmmunition;

    /// <summary>
    /// Swaps the current weapon for a different one. Resets operational status.
    /// </summary>
    /// <param name="weaponType">The new weapon type to install.</param>
    public void ChangeWeapon(ShipWeaponType weaponType)
    {
        WeaponType = weaponType;
        IsOperational = true;
    }

    /// <summary>
    /// Disables the weapon, preventing it from being fired.
    /// </summary>
    public void Disable()
    {
        IsOperational = false;
    }

    /// <summary>
    /// Restores the weapon to operational status.
    /// </summary>
    public void Repair()
    {
        IsOperational = true;
    }

    /// <summary>
    /// Triggers the weapon's cooldown period.
    /// </summary>
    public void ApplyCooldown()
    {

    }
}
