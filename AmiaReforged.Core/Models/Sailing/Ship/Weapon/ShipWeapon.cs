using AmiaReforged.Core.Models.Sailing.Ship.Types;

namespace AmiaReforged.Core.Models.Sailing.Ship.Weapon;

public sealed record ShipWeapon(
    ShipWeaponType Type,
    string ResRef,
    string DisplayName,
    int Damage,
    TimeSpan Cooldown,
    float Range,
    WeaponArc Arc,
    HashSet<ShipAmmunitionType> ValidAmmunition)
    {
        public bool IsValid(ShipAmmunitionType ammunitionType)
            => ValidAmmunition.Contains(ammunitionType);
    }
