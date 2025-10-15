using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Crafting.Models.DifficultyClassCalculation;

[ComputationRuleFor(Property = ItemPropertyType.DamageResistance)]
public class DamageResistCalculation : IComputableDifficulty
{
    public int CalculateDifficultyClass(CraftingProperty property)
    {
        PropertyData.DamageResistance damageResist = new(property);

        return 10 + 2 * damageResist.ResistanceValue;
    }
}
