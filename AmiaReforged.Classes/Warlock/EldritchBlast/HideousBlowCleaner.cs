using AmiaReforged.Classes.Warlock.EldritchBlast.Shape;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast;

[ServiceBinding(typeof(HideousBlowCleaner))]
public class HideousBlowCleaner
{
    public HideousBlowCleaner()
    {
        NwModule.Instance.OnPlayerEquipItem += CleanHideousBlow;
    }

    private void CleanHideousBlow(ModuleEvents.OnPlayerEquipItem eventData)
    {
        if (eventData.Player is not { } playerCreature
            || eventData.Item is not { } item
            || eventData.Slot is not (InventorySlot.RightHand or InventorySlot.LeftHand)
            || playerCreature.HasSpellUse(((Spell)ShapeType.HideousBlow)!) &&
            eventData.Slot != InventorySlot.LeftHand)
            return;

        foreach (ItemProperty ip in item.ItemProperties)
        {
            if (ip.Tag == nameof(ShapeType.HideousBlow))
                item.RemoveItemProperty(ip);
        }
    }
}
