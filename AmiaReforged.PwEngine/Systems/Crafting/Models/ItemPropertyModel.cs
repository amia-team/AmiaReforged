using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models;

public class ItemPropertyModel
{
    public required ItemProperty Property { get; init; }
    public string Label => ItemPropertyHelper.GameLabel(Property);
    public string BasePropertyLabel => GetBasePropertyLabel();

    private string GetBasePropertyLabel()
    {
        string baseLabel = string.Empty;
        if (Property.Property.GameStrRef == null) return baseLabel;

        baseLabel += Property.Property.GameStrRef;
        return baseLabel;
    }
    
    public string SubTypeName => GetSubTypeName();

    private string GetSubTypeName() => Property.SubType?.Name.ToString() ?? string.Empty;

    public string PropertyBonus => GetPropertyBonus();

    private string GetPropertyBonus() =>
        (Property.CostTableValue == null ? string.Empty : Property.CostTableValue.Label) ?? string.Empty;

    public string PropertyParam => GetPropertyParam();

    private string GetPropertyParam() =>
        (Property.Param1TableValue == null ? string.Empty : Property.Param1TableValue.Label) ?? string.Empty;

    public required int GoldCost { get; set; }


    // implicit operator to map back to ItemProperty
    public static implicit operator ItemProperty(ItemPropertyModel model) => model.Property;
}