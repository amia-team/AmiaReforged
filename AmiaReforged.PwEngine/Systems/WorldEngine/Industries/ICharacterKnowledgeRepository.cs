using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.Characters.CharacterData;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public interface ICharacterKnowledgeRepository
{
    List<CharacterKnowledge> GetKnowledgeForIndustry(string industryTag, Guid characterId);
    void Add(CharacterKnowledge ck);
    bool AlreadyKnows(Guid membershipCharacterId, Knowledge tag);
    List<Knowledge> GetAllKnowledge(Guid getId);

    void SaveChanges();
}
