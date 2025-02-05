using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.DifficultyClassCalculation;

[ComputationRuleFor(Property = ItemPropertyType.SavingThrowBonus)]
public class SavingThrowCalculation : IComputableDifficulty
{
    public int CalculateDifficultyClass(CraftingProperty property)
    {
        PropertyData.SavingThrow savingThrow = new(property);

        return 10 + 4 * savingThrow.Bonus;
    }
}