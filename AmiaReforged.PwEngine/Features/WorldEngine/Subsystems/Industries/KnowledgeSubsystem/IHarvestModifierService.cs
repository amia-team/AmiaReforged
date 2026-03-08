using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

public interface IHarvestModifierService
{
    List<KnowledgeHarvestEffect> GetKnowledgeModifiersForNode(string nodeTag, ResourceType resourceType);
    void UpdateKnowledgeRegistry();
}
