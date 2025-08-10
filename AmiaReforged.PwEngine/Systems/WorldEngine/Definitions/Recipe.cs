namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;

public class Recipe
{
    public required string Name { get; init; }
    public int BaseAmount { get; init; } = 1;
    public AmountModifierEnum AmountModifier { get; init; } = AmountModifierEnum.None;
    public QualityModifierEnum QualityModifier { get; init; } = QualityModifierEnum.AverageQualities;
    
    /// <summary>
    /// If not properly assigned and the list is empty, the game will ignore this recipe.
    /// </summary>
    public IEnumerable<RecipeItem> RequiredItems { get; init; } = [];

    /// <summary>
    /// Required to find the definition of the item this recipe creates.
    /// </summary>
    public required string ItemTag { get; init; }

    /// <summary>
    ///  Never null. Recipe does not populate in workshops that ask for it if the tag could not be found in the list of item definitions.
    /// </summary>
    public EconomyItem Output { get; init; } = null!;
}