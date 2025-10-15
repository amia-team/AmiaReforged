using AmiaReforged.PwEngine.Features.WorldEngine.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Items.ItemData;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters;

public interface ICharacterInventoryContext
{
    void AddItem(ItemDto item);
    List<ItemSnapshot> GetInventory();
    Dictionary<EquipmentSlots, ItemSnapshot?> GetEquipment();
}
