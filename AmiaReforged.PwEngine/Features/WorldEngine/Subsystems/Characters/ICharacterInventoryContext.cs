using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

public interface ICharacterInventoryContext
{
    void AddItem(ItemDto item);
    List<ItemSnapshot> GetInventory();
    Dictionary<EquipmentSlots, ItemSnapshot?> GetEquipment();
}
