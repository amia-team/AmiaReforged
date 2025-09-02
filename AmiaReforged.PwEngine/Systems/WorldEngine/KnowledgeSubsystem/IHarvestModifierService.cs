using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;

public interface IHarvestModifierService
{
    List<KnowledgeHarvestEffect> GetKnowledgeModifiersForNode(string nodeTag);
    void UpdateKnowledgeRegistry();
}
