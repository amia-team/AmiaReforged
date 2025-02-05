using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.DifficultyClassCalculation;

[ComputationRuleFor(Property = ItemPropertyType.BonusSpellSlotOfLevelN)]
public class BonusSpellSlotCalculation : IComputableDifficulty
{
    public int CalculateDifficultyClass(CraftingProperty property)
    {
        PropertyData.BonusSpellSlot bonusSpellSlot = new(property);

        return 10 + 3 * bonusSpellSlot.Level;
    }
}