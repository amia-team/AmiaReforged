using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.DamageBonus)]
public class DamageBonusValidator : IValidationRule
{
    private List<string> _elementalTypes = new()
    {
        "Acid",
        "Cold",
        "Electricity",
        "Fire",
        "Sonic"
    };

    private List<string> _physicalTypes = new()
    {
        "Bludgeoning",
        "Piercing",
        "Slashing"
    };

    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        // We just don't want it to already exist on the item or in the changelist
        bool alreadyExists = itemProperties.Any(x => x.Property.PropertyType == ItemPropertyType.DamageBonus);
        bool anyRemoved = changelistProperties.Any(e =>
            e is { BasePropertyType: ItemPropertyType.DamageBonus, State: ChangeListModel.ChangeState.Removed });
        bool inChangelist = changelistProperties.Any(x =>
            x.BasePropertyType == ItemPropertyType.DamageBonus && x.State != ChangeListModel.ChangeState.Removed);

        ValidationEnum result = alreadyExists && anyRemoved || inChangelist ? ValidationEnum.PropertyNeverStacks : ValidationEnum.Valid;
        string error = result == ValidationEnum.PropertyNeverStacks
            ? "Damage Bonus already exists on this item."
            : string.Empty;

        return new ValidationResult
        {
            Result = result,
            ErrorMessage = error
        };
    }

    private class DamageBonus
    {
        public DamageBonus(ItemProperty itemProperty)
        {
            ItemPropertyModel itemPropertyModel = new()
            {
                Property = itemProperty
            };

            Name = itemPropertyModel.SubTypeName;
            DamageDie = itemPropertyModel.PropertyBonus;
            FullLabel = itemPropertyModel.Label;
        }

        public string Name { get; set; }
        public string DamageDie { get; set; }

        public string FullLabel { get; set; }
    }
}