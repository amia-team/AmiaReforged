using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public interface IIndustryMembershipService
{
    void AddMembership(IndustryMembership membership);
    List<IndustryMembership> GetMemberships(Guid characterGuid);
    RankUpResult RankUp(IndustryMembership membership);

    LearningResult LearnKnowledge(IndustryMembership membership, string tag);

    LearningResult LearnKnowledge(Guid characterId, string knowledgeTag);
    LearningResult CanLearnKnowledge(ICharacter character, IndustryMembership membership, Knowledge knowledge);
    List<Knowledge> AllKnowledge(Guid characterId);
}

public enum RankUpResult
{
    Success,
    InsufficientKnowledge,
    AlreadyMaxedOut,
    IndustryNotFound
}
