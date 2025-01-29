using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.DifficultyClassCalculation;

[ComputationRuleFor(Property = ItemPropertyType.SavingThrowBonusSpecific)]
public class SavingThrowSpecificCalculation : IComputableDifficulty
{
    public int CalculateDifficultyClass(CraftingProperty property)
    {
        PropertyData.SavingThrow savingThrow = new(property);

        return 10 + 4 * savingThrow.Bonus;
    }
}