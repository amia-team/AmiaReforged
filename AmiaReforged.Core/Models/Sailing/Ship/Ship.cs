using AmiaReforged.Core.Models.Sailing.Ship.Armament;
using AmiaReforged.Core.Models.Sailing.Ship.Crew;
using AmiaReforged.Core.Models.Sailing.Ship.Types;
using AmiaReforged.Core.Models.Sailing.Ship.Weapon.Ammunition;

namespace AmiaReforged.Core.Models.Sailing.Ship;

public sealed class Ship
{
    private readonly Dictionary<ShipArmamentSlot, ShipArmament> _armaments;
    private readonly Dictionary<ShipAmmunitionType, ShipAmmunition> _ammunition;

    public Ship(
        string name,
        ShipType type,
        int maximumHull,
        ShipPosition position,
        Heading heading = Heading.East,
        ShipArmament[]? armaments = null,
        ShipAmmunition[]? ammunition = null,
        ShipCrew? crew = null)
    {
        Name = name;
        Type = type;
        MaximumHull = maximumHull;
        Hull = maximumHull;
        Position = position;
        Heading = heading;
        Crew = crew ?? new ShipCrew();
        _armaments = (armaments ?? []).ToDictionary(a => a.Slot);
        _ammunition = (ammunition ?? []).ToDictionary(a => a.Type);
        Id = Guid.NewGuid();
    }

    public string Name { get; }

    /// <summary>
    /// The unique identifier of the ship. Each generated ship object is unique, and you don't need this for reference.
    /// Ship.Id should be used for database operations for persistence if relogging or crashing the server loses
    /// the ship object context or relevant associations.
    /// </summary>
    public Guid Id { get; }

    public ShipType Type { get; }

    public int MaximumHull { get; }

    public int Hull { get; private set; }

    public ShipPosition Position { get; private set; }

    public Heading Heading { get; private set; }

    public ShipCrew Crew { get; private set; }

    public bool IsUnderway { get; private set; }

    public bool IsSunk => Hull <= 0;

    public IReadOnlyCollection<ShipArmament> Armaments => _armaments.Values;

    public IReadOnlyCollection<ShipAmmunition> Ammunition => _ammunition.Values;

    public bool TryGetArmament(ShipArmamentSlot slot, out ShipArmament? armament)
        => _armaments.TryGetValue(slot, out armament);

    public bool TryGetAmmunition(ShipAmmunitionType type, out ShipAmmunition? ammunition)
        => _ammunition.TryGetValue(type, out ammunition);

    public void StartUnderway() => IsUnderway = true;

    public void StopUnderway() => IsUnderway = false;

    public void MoveTo(ShipPosition position) => Position = position;

    public void ChangeHeading(Heading heading) => Heading = heading;

    public void ApplyDamage(int damage)
    {
        if (damage <= 0) return;
        Hull -= damage;
    }

    public void RepairHull(int amount)
    {
        if (amount <= 0) return;
        Hull = Math.Min(MaximumHull, Hull + amount);
    }

    public ShipAttackResult ResolveAttack(Ship target, ShipArmamentSlot armamentSlot, ShipAmmunitionType ammunitionType)
    {
        if (target == this)
            return ShipAttackResult.NoTarget;
        if (IsSunk)
            return ShipAttackResult.AttackerDisabled;
        if (target.IsSunk)
            return ShipAttackResult.TargetDisabled;
        if (!TryGetArmament(armamentSlot, out ShipArmament? armament) || armament == null)
            return ShipAttackResult.NoWeapon;
        if (!armament.IsOperational)
            return ShipAttackResult.AttackerDisabled;
        if (!TryGetAmmunition(ammunitionType, out ShipAmmunition? ammunition)
            || ammunition == null || ammunition.Quantity <= 0)
            return ShipAttackResult.NoAmmunition;
        if (Position.DistanceTo(target.Position) > armament.Range)
            return ShipAttackResult.OutOfRange;

        ammunition.UseAmmo();
        armament.ApplyCooldown();
        return ShipAttackResult.Hit;
    }
}
