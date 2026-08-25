namespace AmiaReforged.Core.Models.Sailing.Ship.Weapon.Ammunition;

public sealed class ShipAmmunition
{
    public required ShipAmmunitionType Type { get; init; }

    public int Quantity { get; private set; }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Quantity += amount;
    }

    public void UseAmmo(int amount = 1)
    {
        if (amount <= 0 || Quantity < amount) return;
        Quantity -= amount;
    }
}
