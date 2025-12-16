namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

public interface IHarvestModifierService
{
    List<KnowledgeHarvestEffect> GetKnowledgeModifiersForNode(string nodeTag);
    void UpdateKnowledgeRegistry();
}
