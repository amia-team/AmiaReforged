using AmiaReforged.PwEngine.Features.WorldEngine.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters;

public interface ICharacterKnowledgeContext
{
    int GetKnowledgePoints();
    void AddKnowledgePoints(int points);
    void SubtractKnowledgePoints(int points);
    List<Knowledge> AllKnowledge();
    LearningResult Learn(string knowledgeTag);
    bool CanLearn(string knowledgeTag);
    List<KnowledgeHarvestEffect> KnowledgeEffectsForResource(string definitionTag);
}
