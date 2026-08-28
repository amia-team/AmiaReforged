using AmiaReforged.Core.Models.Sailing.Ship.Types;

namespace AmiaReforged.Core.Models.Sailing.Ship.Weapon.Ammunition;

/// <summary>
/// Manages a specific type of ammunition stored on a ship, including its current quantity.
/// </summary>
public sealed class ShipAmmunition
{
    /// <summary>
    /// The specific type of ammunition (e.g., Roundshot, Grapeshot).
    /// </summary>
    public required ShipAmmunitionType Type { get; init; }

    /// <summary>
    /// The current number of units of this ammunition available on the ship.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Adds ammunition to the ship's stores.
    /// </summary>
    /// <param name="amount">The quantity to add. Must be positive.</param>
    public void Add(int amount)
    {
        if (amount <= 0) return;
        Quantity += amount;
    }

    /// <summary>
    /// Deducts a specified amount of ammunition from the ship's stores.
    /// </summary>
    /// <param name="amount">The quantity to use. Defaults to 1.</param>
    public void UseAmmo(int amount = 1)
    {
        if (amount <= 0 || Quantity < amount) return;
        Quantity -= amount;
    }
}
