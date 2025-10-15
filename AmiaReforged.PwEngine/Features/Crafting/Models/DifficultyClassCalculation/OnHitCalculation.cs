using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Features.Crafting.Models.DifficultyClassCalculation;

[ComputationRuleFor(Property = ItemPropertyType.OnHitProperties)]
public class OnHitCalculation : IComputableDifficulty
{
    public int CalculateDifficultyClass(CraftingProperty property)
    {
        PropertyData.OnHit incoming = new(property);
        LogManager.GetCurrentClassLogger().Info($"incoming: {incoming.Type}");
        return 0;
    }
}
