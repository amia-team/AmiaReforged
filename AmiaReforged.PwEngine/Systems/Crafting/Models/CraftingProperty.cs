using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models;

/// <summary>
///     Plain old data object for crafting properties. Should not contain any logic (i.e.: Can the property stack).
/// </summary>
public class CraftingProperty
{
    public NuiButton Button;
    public required ItemProperty ItemProperty { get; init; }
    public required string GuiLabel { get; init; }
    public required int PowerCost { get; init; }
    public string GameLabel => ItemPropertyHelper.GameLabel(ItemProperty);
    public required CraftingTier CraftingTier { get; set; }

    public bool Removable { get; set; } = true;

    public int GoldCost { get; set; }

    public ItemPropertyModel ToItemPropertyModel() =>
        new()
        {
            Property = ItemProperty,
            GoldCost = GoldCost
        };

    // operator to convert back to ItemProperty
    public static implicit operator ItemProperty(CraftingProperty craftingProperty) => craftingProperty.ItemProperty;

    //operator to convert to ItemPropertyModel
    public static implicit operator ItemPropertyModel(CraftingProperty craftingProperty) =>
        craftingProperty.ToItemPropertyModel();
}