namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;

public interface IItemDefinitionRepository
{
    void AddItemDefinition(ItemData.ItemBlueprint definition);
    ItemData.ItemBlueprint? GetByTag(string harvestOutputItemDefinitionTag);
    ItemData.ItemBlueprint? GetByResRef(string resRef);
    List<ItemData.ItemBlueprint> AllItems();
    
    /// <summary>
    /// Finds tags that are similar to the given tag (for error message suggestions).
    /// </summary>
    List<string> FindSimilarTags(string tag, int maxResults = 3);
}
