﻿using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.DamageResistance)]
public class ResistanceValidationRules : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        ValidationEnum result = ValidationEnum.Valid;
        string error = string.Empty;

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
        bool alreadyExists = allResistances.Any(x => x.ResistanceType == resistance.ResistanceType);
        result = alreadyExists ? ValidationEnum.CannotStackSameSubtype : ValidationEnum.Valid;
        error = alreadyExists ? "Resistance already exists on this item." : string.Empty;

        return new ValidationResult
        {
            Enum = result,
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
            ResistanceValue = int.Parse(incoming.PropertyBonus);
        }

        public string ResistanceType { get; set; }

        public int ResistanceValue { get; set; }
    }
}