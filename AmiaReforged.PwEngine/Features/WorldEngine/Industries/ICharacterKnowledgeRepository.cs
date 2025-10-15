using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Industries;

public interface ICharacterKnowledgeRepository
{
    List<CharacterKnowledge> GetKnowledgeForIndustry(string industryTag, Guid characterId);
    void Add(CharacterKnowledge ck);
    bool AlreadyKnows(Guid membershipCharacterId, Knowledge tag);
    List<Knowledge> GetAllKnowledge(Guid getId);

    void SaveChanges();
}
