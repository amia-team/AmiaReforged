namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.KnowledgeSubsystem;

public interface IHarvestModifierService
{
    List<KnowledgeHarvestEffect> GetKnowledgeModifiersForNode(string nodeTag);
    void UpdateKnowledgeRegistry();
}
