using AmiaReforged.Core.Models.Sailing.Ship.Types;

namespace AmiaReforged.Core.Models.Sailing.Ship.Cargo;

public sealed class ShipCargo
{
    public sealed record CargoItem(CargoItemType ItemType, int Quantity);

    private readonly Dictionary<CargoItemType, int> _items = new();

    public ShipCargo(int capacity)
    {
        if (capacity < 0) return;
        Capacity = capacity;
    }

    public int Capacity { get; }

    public int UsedCapacity => _items.Values.Sum();

    public int RemainingCapacity => Capacity - UsedCapacity;

    public bool IsFull => RemainingCapacity == 0;

    public IReadOnlyCollection<CargoItem> Items
        => _items.Select(pair => new CargoItem(pair.Key, pair.Value)).ToArray();

    public int GetQuantity(CargoItemType itemType) => _items.GetValueOrDefault(itemType);

    public bool TryAdd(CargoItemType itemType, int quantity)
    {
        if (quantity <= 0 || quantity > RemainingCapacity)
            return false;

        _items[itemType] = GetQuantity(itemType) + quantity;
        return true;
    }

    public bool TryRemove(CargoItemType itemType, int quantity)
    {
        if (quantity <= 0 || GetQuantity(itemType) < quantity)
            return false;

        int remaining = GetQuantity(itemType) - quantity;

        if (remaining == 0)
            _items.Remove(itemType);
        else
            _items[itemType] = remaining;

        return true;
    }
}
