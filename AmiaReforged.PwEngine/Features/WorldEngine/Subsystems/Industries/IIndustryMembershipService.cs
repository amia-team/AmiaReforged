using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

public interface IIndustryMembershipService
{
    void AddMembership(IndustryMembership membership);
    List<IndustryMembership> GetMemberships(Guid characterGuid);
    RankUpResult RankUp(IndustryMembership membership);

    LearningResult LearnKnowledge(IndustryMembership membership, string tag);

    LearningResult LearnKnowledge(Guid characterId, string knowledgeTag);
    LearningResult CanLearnKnowledge(ICharacter character, IndustryMembership membership, Knowledge knowledge);
    List<Knowledge> AllKnowledge(Guid characterId);
    RankUpResult RankUp(Guid characterId, string industryTag);
    bool CanLearnKnowledge(Guid characterId, string knowledgeTag);

    /// <summary>
    /// Returns all <see cref="CharacterKnowledge"/> learned by the character, across all industries.
    /// </summary>
    List<CharacterKnowledge> GetAllCharacterKnowledge(Guid characterId);

    /// <summary>
    /// Returns the <see cref="CharacterKnowledge"/> learned by the character for a specific industry.
    /// </summary>
    List<CharacterKnowledge> GetCharacterKnowledgeForIndustry(Guid characterId, string industryTag);
}
