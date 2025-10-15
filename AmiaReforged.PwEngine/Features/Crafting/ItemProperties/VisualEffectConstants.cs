using AmiaReforged.PwEngine.Features.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Crafting.ItemProperties;

public static class VisualEffectConstants
{
    private const int VfxCost = 10000;

    public static readonly CraftingCategory VisualEffects = new(categoryId: "vfx")
    {
        Label = "Visual Effects",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_ACID)!,
                GuiLabel = "Acid",
                GoldCost = VfxCost,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_COLD)!,
                GuiLabel = "Cold",
                GoldCost = VfxCost,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_FIRE)!,
                GuiLabel = "Fire",
                GoldCost = VfxCost,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_ELECTRICAL)!,
                GuiLabel = "Electrical",
                GoldCost = VfxCost,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_SONIC)!,
                GuiLabel = "Sonic",
                GoldCost = VfxCost,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_HOLY)!,
                GuiLabel = "Holy",
                GoldCost = VfxCost,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyVisualEffect(NWScript.ITEM_VISUAL_EVIL)!,
                GuiLabel = "Evil",
                GoldCost = VfxCost,
                CraftingTier = CraftingTier.Greater
            }
        ]
    };
}
