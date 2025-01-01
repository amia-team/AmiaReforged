using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models;

/// <summary>
///  Plain old data object for crafting properties. Should not contain any logic (i.e.: Can the property stack).
/// </summary>
public class CraftingProperty
{
    public NuiButton Button;
    public required ItemProperty ItemProperty { get; init; }
    public required string GuiLabel { get; init; }
    public required int Cost { get; init; }

    public string GameLabel => ItemPropertyHelper.GameLabel(ItemProperty);


    public required CraftingTier CraftingTier { get; set; }

    public NuiComboEntry ToComboEntry(int value)
    {
        return new NuiComboEntry(GuiLabel, value);
    }

    public bool Removeable { get; set; } = true;

    public NuiElement ToNuiElement()
    {
        NuiRow row = new()
        {
            Children =
            {
                new NuiButton($"{GuiLabel} ({Cost} Points)")
                {
                    Id = Guid.NewGuid().ToString()
                }.Assign(out Button)
            }
        };
        
        return row;
    }
}