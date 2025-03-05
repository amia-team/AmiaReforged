using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.BonusSpellSlotOfLevelN)]
public class BonusSpellSlotValidator : IValidationRule
{
    /*
     * Validation rule here is:
     * No more than three bonus spell slots of the same level can be added to an item.
     */
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        BonusSpellSlot incomingBonusSpellSlot = new(incoming);

        // Get all bonus spell slots on the item
        List<BonusSpellSlot> bonusSpellSlots = itemProperties
            .Where(x => x.Property.PropertyType == ItemPropertyType.BonusSpellSlotOfLevelN)
            .Select(x => new BonusSpellSlot(x))
            .ToList();

        // Get all bonus spell slots in the changelist
        List<BonusSpellSlot> bonusSpellSlotsInChangelist = changelistProperties
            .Where(x => x.BasePropertyType == ItemPropertyType.BonusSpellSlotOfLevelN &&
                        x.State != ChangeListModel.ChangeState.Removed)
            .Select(x => new BonusSpellSlot(x.Property))
            .ToList();

        // Combine the two lists
        bonusSpellSlots.AddRange(bonusSpellSlotsInChangelist);

        // Since we know for a fact the incoming property is a bonus spell slot, we can check its level and class against
        // what's already on the item and in the changelist
        int numberOfBonusSpellSlotsOfSameLevel = bonusSpellSlots
            .Count(x => x.Level == incomingBonusSpellSlot.Level && x.Class == incomingBonusSpellSlot.Class);

        ValidationEnum result = numberOfBonusSpellSlotsOfSameLevel >= 3
            ? ValidationEnum.LimitReached
            : ValidationEnum.Valid;
        string error = numberOfBonusSpellSlotsOfSameLevel >= 3
            ? $"No more than three bonus spell slots of level {incomingBonusSpellSlot.Level} can be added to an item."
            : string.Empty;

        return new()
        {
            Result = result,
            ErrorMessage = error
        };
    }

    private class BonusSpellSlot
    {
        public BonusSpellSlot(ItemProperty property)
        {
            ItemPropertyModel model = new()
            {
                Property = property
            };

            Class = model.SubTypeName;
            Level = model.PropertyBonus;
        }

        public string Class { get; }
        public string Level { get; }
    }
}