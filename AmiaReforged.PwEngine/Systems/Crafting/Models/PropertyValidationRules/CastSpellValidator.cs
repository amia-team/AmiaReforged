using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.CastSpell)]
public class CastSpellValidator : IValidationRule
{
    private static readonly string[] Fluff =
    [
        "Aid (3)",
        "Bless (2)",
        "Cat's Grace (3)",
        "Bull's Strength (3)",
        "Endurance (3)",
        "Expeditious Retreat (5)",
        "Light (1)"
    ];

    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        CastSpell castSpell = new(incoming.ItemProperty);

        ValidationResult res = Fluff.Contains(castSpell.SpellName)
            ? ValidateFluff(castSpell, itemProperties, changelistProperties)
            : ValidateNonFluff(castSpell, itemProperties, changelistProperties);

        return res;
    }

    private ValidationResult ValidateFluff(CastSpell castSpell, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        // Get all of the cast spell properties in the changelist and on the item and combine them
        List<CastSpell> castSpellsInChangelist = changelistProperties
            .Where(e => e.BasePropertyType == ItemPropertyType.CastSpell &&
                        e.State != ChangeListModel.ChangeState.Removed)
            .Select(p => new CastSpell(p.Property))
            .ToList();
        List<CastSpell> castSpellsInItem = itemProperties
            .Where(x => x.Property.PropertyType == ItemPropertyType.CastSpell)
            .Select(p => new CastSpell(p))
            .ToList();

        List<CastSpell> allCastSpells = castSpellsInChangelist.Concat(castSpellsInItem).ToList();

        // Look for any fluff spells in the item or changelist
        bool fluffExists = allCastSpells.Any(x => Fluff.Contains(x.SpellName));

        return new ValidationResult
        {
            Result = fluffExists ? ValidationEnum.LimitReached : ValidationEnum.Valid,
            ErrorMessage = fluffExists ? "Only one fluff spell can be added to an item." : string.Empty
        };
    }

    private ValidationResult ValidateNonFluff(CastSpell castSpell, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        // Get all of the cast spell properties in the changelist and on the item and combine them
        List<CastSpell> castSpellsInChangelist = changelistProperties
            .Where(e => e.BasePropertyType == ItemPropertyType.CastSpell &&
                        e.State != ChangeListModel.ChangeState.Removed)
            .Select(p => new CastSpell(p.Property))
            .ToList();
        List<CastSpell> castSpellsInItem = itemProperties
            .Where(x => x.Property.PropertyType == ItemPropertyType.CastSpell)
            .Select(p => new CastSpell(p))
            .ToList();

        List<CastSpell> allCastSpells = castSpellsInChangelist.Concat(castSpellsInItem).ToList();

        // Now we only care if the same spell exists in the item or the changelist
        bool alreadyExists = allCastSpells.Count != 0;

        return new ValidationResult
        {
            Result = alreadyExists ? ValidationEnum.CannotStackSameSubtype : ValidationEnum.Valid,
            ErrorMessage = alreadyExists ? $"{castSpell.SpellName} already exists on the item." : string.Empty
        };
    }

    private class CastSpell
    {
        public CastSpell(ItemProperty itemProperty)
        {
            ItemPropertyModel incoming = new()
            {
                Property = itemProperty
            };

            SpellName = incoming.SubTypeName;
            UsesPerDay = incoming.PropertyBonus;
        }

        public string SpellName { get; }
        public string UsesPerDay { get; set; }
    }
}