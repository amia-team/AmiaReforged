using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

public interface IInventoryPort
{
    void AddItem(ItemDto item);
    List<ItemSnapshot> GetInventory();
    Dictionary<EquipmentSlots, ItemSnapshot?> GetEquipment();
}