using AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting;

[ServiceBinding(typeof(CraftingPropertyData))]
public class CraftingPropertyData
{
    public Dictionary<int, IReadOnlyCollection<CraftingProperty>> Properties { get; } = new();

    public CraftingPropertyData()
    {
        SetupAmulets();
    }

    private void SetupAmulets()
    {
        // This list of properties is different because natural armor has its own unique costs.
        List<CraftingProperty> properties = new()
        {
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(1)!,
                GuiLabel = "+1",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(2)!,
                GuiLabel = "+2",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(3)!,
                GuiLabel = "+3",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(4)!,
                GuiLabel = "+4",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertyACBonus(5)!,
                GuiLabel = "+5",
                CraftingTier = CraftingTier.Flawless
            }
        };
        properties.AddRange(GenericItemProperties.ElementalResistances);
        properties.AddRange(GenericItemProperties.DamageReductions);
        Properties.Add(NWScript.BASE_ITEM_AMULET, properties);
    }
}