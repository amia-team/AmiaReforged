namespace AmiaReforged.PwEngine.Features.WorldEngine.KnowledgeSubsystem;

public interface IHarvestModifierService
{
    List<KnowledgeHarvestEffect> GetKnowledgeModifiersForNode(string nodeTag);
    void UpdateKnowledgeRegistry();
}
