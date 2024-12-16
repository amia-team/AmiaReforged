using AmiaReforged.PwEngine.Systems.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class VisualEffectConstants
{
    public static readonly CraftingPropertyCategory VisualEffects = new()
    {
        Label = "Visual Effects",
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_ACID)!,
                GuiLabel = "Acid",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_COLD)!,
                GuiLabel = "Cold",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_FIRE)!,
                GuiLabel = "Fire",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_ELECTRICAL)!,
                GuiLabel = "Electrical",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_SONIC)!,
                GuiLabel = "Sonic",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_HOLY)!,
                GuiLabel = "Holy",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_EVIL)!,
                GuiLabel = "Evil",
                CraftingTier = CraftingTier.Greater
            },
        }
    };
}