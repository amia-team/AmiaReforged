using Anvil.API;

namespace AmiaReforged.Classes.Associates;

public static class ShadowAppearance
{
    public static bool ApplyShadowAppearance(NwCreature shadow, NwCreature shadowDancer)
    {
        if (!shadow.ResRef.StartsWith("sd_shadow_")) return false;

        shadow.Appearance = shadowDancer.Appearance;
        shadow.Gender = shadowDancer.Gender;
        shadow.WingType = shadowDancer.WingType;
        shadow.TailType = shadowDancer.TailType;
        shadow.VisualTransform.Scale = shadowDancer.VisualTransform.Scale;

        foreach (CreaturePart part in Enum.GetValues(typeof(CreaturePart)))
        {
            shadow.SetCreatureBodyPart(part,shadowDancer.GetCreatureBodyPart(part));
        }

        foreach (ColorChannel color in Enum.GetValues(typeof(ColorChannel)))
        {
            shadow.SetColor(color, shadowDancer.GetColor(color));
        }

        InventorySlot[] slotsToClone =
        [
            InventorySlot.Chest,
            InventorySlot.Head,
            InventorySlot.Cloak
        ];

        foreach (InventorySlot slot in slotsToClone)
        {
            NwItem? originalItem = shadowDancer.GetItemInSlot(slot);
            if (originalItem == null) continue;

            NwItem clonedItem = originalItem.Clone(shadow);
            clonedItem.RemoveItemProperties();
            clonedItem.Droppable = false;
            shadow.RunEquip(clonedItem, slot);
        }

        return true;
    }
}
