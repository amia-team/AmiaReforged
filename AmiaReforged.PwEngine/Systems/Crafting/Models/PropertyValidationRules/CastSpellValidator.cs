using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.CastSpell)]
public class CastSpellValidator : IValidationRule
{
    private static readonly string[] Beneficial = new[]
    {
        "Aura of Vitality",
        "Belagarn's Iron Horn",
        "Barkskin",
        "War Cry",
        "Bless Weapon",
        "Cat's Grace",
        "Haste",
        "Improved Invisibility"
    };

    private static readonly string[] Fluff = new[]
    {
        "Aid",
        "Bless",
        "Bull's Strength",
        "Cure Critical Wounds",
    };

    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        ValidationEnum result = ValidationEnum.Valid;
        string error = string.Empty;

        CastSpell castSpell = new(incoming.ItemProperty);

        LogManager.GetCurrentClassLogger().Info($"{castSpell.SpellName} {castSpell.UsesPerDay}");

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
        bool alreadyExists = allCastSpells.Any(x => x.SpellName == castSpell.SpellName);
        result = alreadyExists ? ValidationEnum.CannotStackSameSubtype : ValidationEnum.Valid;


        return new ValidationResult
        {
            Result = result,
            ErrorMessage = error
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

        public string SpellName { get; set; }
        public string UsesPerDay { get; set; }
    }
}