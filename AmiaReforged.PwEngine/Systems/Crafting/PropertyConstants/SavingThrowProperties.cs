using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class SavingThrowProperties
{
    /// <summary>
    /// Specific Saves for Perfect mythals.
    /// </summary>
    public static readonly IReadOnlyList<CraftingProperty> SpecificSaves = new[]
    {
        // Perfect only...Costs 2.
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_ACID, 6)!,
            GuiLabel = "+6 vs Acid",
            CraftingTier = CraftingTier.Perfect
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_COLD, 6)!,
            GuiLabel = "+6 vs Cold",
            CraftingTier = CraftingTier.Perfect
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_ELECTRICAL, 6)!,
            GuiLabel = "+6 vs Electrical",
            CraftingTier = CraftingTier.Perfect
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_FIRE, 6)!,
            GuiLabel = "+6 vs Fire",
            CraftingTier = CraftingTier.Perfect
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_SONIC, 6)!,
            GuiLabel = "+6 vs Sonic",
            CraftingTier = CraftingTier.Perfect
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_NEGATIVE, 6)!,
            GuiLabel = "+6 vs Negative",
            CraftingTier = CraftingTier.Perfect
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_POISON, 6)!,
            GuiLabel = "+6 vs Poison",
            CraftingTier = CraftingTier.Perfect
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_POSITIVE, 6)!,
            GuiLabel = "+6 vs Positive",
            CraftingTier = CraftingTier.Perfect
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_FEAR, 6)!,
            GuiLabel = "+6 vs Fear",
            CraftingTier = CraftingTier.Perfect
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DISEASE, 6)!,
            GuiLabel = "+6 vs Disease",
            CraftingTier = CraftingTier.Perfect
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DIVINE, 6)!,
            GuiLabel = "+6 vs Divine",
            CraftingTier = CraftingTier.Perfect
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_MINDAFFECTING, 6)!,
            GuiLabel = "+6 vs Mind-Affecting",
            CraftingTier = CraftingTier.Perfect
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DEATH, 6)!,
            GuiLabel = "+6 vs Trap",
            CraftingTier = CraftingTier.Perfect
        }
    };
}