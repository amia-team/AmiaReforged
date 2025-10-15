using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.DamageResistance)]
public class DamageResistValidator : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        Resistance resistance = new(incoming.ItemProperty);

        // Get all resistances in the changelist and on the item
        List<Resistance> resistancesInChangelist = changelistProperties
            .Where(e => e.BasePropertyType == ItemPropertyType.DamageResistance &&
                        e.State != ChangeListModel.ChangeState.Removed)
            .Select(p => new Resistance(p.Property))
            .ToList();

        List<Resistance> resistancesInItem = itemProperties
            .Where(x => x.Property.PropertyType == ItemPropertyType.DamageResistance)
            .Select(p => new Resistance(p))
            .ToList();

        // Just combine them
        List<Resistance> allResistances = resistancesInChangelist.Concat(resistancesInItem).ToList();

        // Now we only care if the same type exists in the item or the changelist
        bool removed = changelistProperties
            .Where(e => e is
                { BasePropertyType: ItemPropertyType.DamageResistance, State: ChangeListModel.ChangeState.Removed })
            .Select(e => new Resistance(e.Property)).Any(r => r.ResistanceType == resistance.ResistanceType);
        bool alreadyExists = allResistances.Any(x => x.ResistanceType == resistance.ResistanceType);
        ValidationEnum result =
            alreadyExists && !removed ? ValidationEnum.CannotStackSameSubtype : ValidationEnum.Valid;
        string error = alreadyExists
            ? $"{resistance.ResistanceType} Resistance already exists on this item."
            : string.Empty;

        return new ValidationResult
        {
            Result = result,
            ErrorMessage = error
        };
    }

    private class Resistance
    {
        public Resistance(ItemProperty itemProperty)
        {
            ItemPropertyModel incoming = new()
            {
                Property = itemProperty
            };

            ResistanceType = incoming.SubTypeName;

            // Splits Resist_5/- into its constituent parts and selects the numeric component
            ResistanceValue = int.Parse(incoming.PropertyBonus.Split(separator: "_")[1].Split(separator: "/")[0]);
        }

        public string ResistanceType { get; }

        public int ResistanceValue { get; set; }
    }
}
