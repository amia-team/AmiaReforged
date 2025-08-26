using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public interface IIndustryMembershipService
{
    void AddMembership(IndustryMembership membership);
    List<IndustryMembership> GetMemberships(Guid characterGuid);
    RankUpResult RankUp(IndustryMembership membership);

    LearningResult LearnKnowledge(IndustryMembership membership, string tag);

    LearningResult CanLearnKnowledge(ICharacter character, IndustryMembership membership, Knowledge knowledge);
}

public enum RankUpResult
{
    Success,
    InsufficientKnowledge,
    AlreadyMaxedOut,
    IndustryNotFound
}
