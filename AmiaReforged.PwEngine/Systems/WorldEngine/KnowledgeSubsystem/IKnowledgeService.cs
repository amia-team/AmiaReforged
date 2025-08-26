using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;

public interface IKnowledgeService
{
    bool AddKnowledge(Guid characterGuid, string tag);
    bool RemoveKnowledge(Guid characterGuid, string tag);
    bool HasKnowledge(Guid characterGuid, string tag);
    List<string> GetTags(Guid characterGuid);
    List<CharacterKnowledge> GetKnowledge(Guid characterGuid);
}
