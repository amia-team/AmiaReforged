namespace AmiaReforged.PwEngine.Systems.Crafting.Models.DifficultyClassCalculation;

/// <summary>
///     Must be implemented by any class decorated by <see cref="ComputationRuleFor" />.
/// </summary>
public interface IComputableDifficulty
{
    public int CalculateDifficultyClass(CraftingProperty property);
}