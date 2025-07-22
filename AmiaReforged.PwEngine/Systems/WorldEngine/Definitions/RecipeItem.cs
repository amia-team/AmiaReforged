using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;

public class RecipeItem
{
    
    public EconomyItemInstance Item { get; set; }

    /// <summary>
    /// The amount of an item needed to make an object. Defaults to 1.
    /// </summary>
    public int Amount { get; set; } = 1;

    /// <summary>
    /// Dictates the minimum quality required be at least this amount.
    /// If it is 'None,' quality is not a factor in the recipe at all.
    /// </summary>
    public QualityEnum RequiredQuality { get; set; } = QualityEnum.None;
}