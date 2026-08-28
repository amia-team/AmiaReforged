using AmiaReforged.Core.Models.Sailing.Ship.Armament;
using AmiaReforged.Core.Models.Sailing.Ship.Cargo;
using AmiaReforged.Core.Models.Sailing.Ship.Crew;
using AmiaReforged.Core.Models.Sailing.Ship.Types;
using AmiaReforged.Core.Models.Sailing.Ship.Weapon.Ammunition;

namespace AmiaReforged.Core.Models.Sailing.Ship;

/// <summary>
/// Represents a sailing vessel in the world. This is the primary object for managing ship state,
/// movement, combat, and crew.
/// </summary>
public sealed class Ship
{
    private readonly Dictionary<ShipArmamentSlot, ShipArmament> _armaments;
    private readonly Dictionary<ShipAmmunitionType, ShipAmmunition> _ammunition;

    /// <summary>
    /// Initializes a new instance of a Ship.
    /// </summary>
    /// <param name="name">The display name of the ship.</param>
    /// <param name="type">The template or class of the ship (e.g., Sloop, Frigate).</param>
    /// <param name="maximumHull">The maximum health points of the ship's hull.</param>
    /// <param name="position">The initial world position of the ship.</param>
    /// <param name="heading">The direction the ship is facing. Defaults to East.</param>
    /// <param name="armaments">Initial set of weapons installed on the ship.</param>
    /// <param name="ammunition">Initial stock of ammunition types.</param>
    /// <param name="crew">The crew assigned to the ship. If null, a new empty crew is created.</param>
    /// <param name="cargoCapacity">The maximum number of cargo units the ship can carry.</param>
    public Ship(
        string name,
        ShipType type,
        int maximumHull,
        ShipPosition position,
        Heading heading = Heading.East,
        ShipArmament[]? armaments = null,
        ShipAmmunition[]? ammunition = null,
        ShipCrew? crew = null,
        int cargoCapacity = 50)
    {
        Name = name;
        Type = type;
        MaximumHull = maximumHull;
        Hull = maximumHull;
        Position = position;
        Heading = heading;
        Crew = crew ?? new ShipCrew();
        Cargo = new ShipCargo(cargoCapacity);
        _armaments = (armaments ?? []).ToDictionary(a => a.Slot);
        _ammunition = (ammunition ?? []).ToDictionary(a => a.Type);
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// The display name of the ship.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The unique identifier of the ship. Each generated ship object is unique, and you don't need this for reference.
    /// Ship.Id should be used for database operations for persistence if relogging or crashing the server loses
    /// the ship object context or relevant associations.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// The classification of the ship, determining its base statistics and appearance.
    /// </summary>
    public ShipType Type { get; }

    /// <summary>
    /// The maximum integrity points the hull can have.
    /// </summary>
    public int MaximumHull { get; }

    /// <summary>
    /// The current integrity points of the hull. If this reaches 0, the ship is considered sunk.
    /// </summary>
    public int Hull { get; private set; }

    /// <summary>
    /// The current location of the ship in the world.
    /// </summary>
    public ShipPosition Position { get; private set; }

    /// <summary>
    /// The direction the ship is currently facing.
    /// </summary>
    public Heading Heading { get; private set; }

    /// <summary>
    /// The crew members currently assigned to the ship.
    /// </summary>
    public ShipCrew Crew { get; private set; }

    /// <summary>
    /// The cargo currently carried by the ship.
    /// </summary>
    public ShipCargo Cargo { get; }

    /// <summary>
    /// Indicates whether the ship is currently moving through the water.
    /// </summary>
    public bool IsUnderway { get; private set; }

    /// <summary>
    /// Returns true if the ship's hull has been reduced to zero or less.
    /// </summary>
    public bool IsSunk => Hull <= 0;

    /// <summary>
    /// A collection of all weapons currently installed on the ship.
    /// </summary>
    public IReadOnlyCollection<ShipArmament> Armaments => _armaments.Values;

    /// <summary>
    /// A collection of all ammunition types currently stored on the ship.
    /// </summary>
    public IReadOnlyCollection<ShipAmmunition> Ammunition => _ammunition.Values;

    /// <summary>
    /// Attempts to find a weapon installed in the specified slot.
    /// </summary>
    /// <param name="slot">The physical slot to check (e.g., Bow, Port, Starboard).</param>
    /// <param name="armament">The armament found in the slot, or null if none.</param>
    /// <returns>True if an armament exists in the specified slot.</returns>
    public bool TryGetArmament(ShipArmamentSlot slot, out ShipArmament? armament)
        => _armaments.TryGetValue(slot, out armament);

    /// <summary>
    /// Attempts to find ammunition of the specified type in the ship's stores.
    /// </summary>
    /// <param name="type">The type of ammunition to look for.</param>
    /// <param name="ammunition">The ammunition object if found, or null if none.</param>
    /// <returns>True if the ship has a record for this ammunition type.</returns>
    public bool TryGetAmmunition(ShipAmmunitionType type, out ShipAmmunition? ammunition)
        => _ammunition.TryGetValue(type, out ammunition);

    /// <summary>
    /// Sets the ship's status to underway (moving).
    /// </summary>
    public void StartUnderway() => IsUnderway = true;

    /// <summary>
    /// Stops the ship's movement.
    /// </summary>
    public void StopUnderway() => IsUnderway = false;

    /// <summary>
    /// Updates the ship's position in the world.
    /// </summary>
    public void MoveTo(ShipPosition position) => Position = position;

    /// <summary>
    /// Changes the direction the ship is facing.
    /// </summary>
    public void ChangeHeading(Heading heading) => Heading = heading;

    /// <summary>
    /// Reduces the ship's hull integrity by the specified amount.
    /// </summary>
    /// <param name="damage">The amount of damage to apply. Must be positive.</param>
    public void ApplyDamage(int damage)
    {
        if (damage <= 0) return;
        Hull -= damage;
    }

    /// <summary>
    /// Increases the ship's hull integrity, up to its MaximumHull.
    /// </summary>
    /// <param name="amount">The amount of health to restore. Must be positive.</param>
    public void RepairHull(int amount)
    {
        if (amount <= 0) return;
        Hull = Math.Min(MaximumHull, Hull + amount);
    }

    /// <summary>
    /// Resolves an attack attempt against a target ship using a specific weapon and ammunition.
    /// Checks for range, operational status, and ammunition availability.
    /// </summary>
    /// <param name="target">The ship being attacked.</param>
    /// <param name="armamentSlot">The slot of the weapon to fire.</param>
    /// <param name="ammunitionType">The type of ammunition to use.</param>
    /// <returns>A result indicating Hit, OutOfRange, NoAmmunition, etc.</returns>
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
