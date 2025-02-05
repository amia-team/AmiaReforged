using AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.DifficultyClassCalculation;

[ComputationRuleFor(Property = ItemPropertyType.DamageBonus)]
public class DamageBonusCalculation : IComputableDifficulty
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public int CalculateDifficultyClass(CraftingProperty property)
    {
        PropertyData.DamageBonus damageBonus = new(property);

        return 10 + 3 * damageBonus.DieSize * damageBonus.NumDie;
    }
}