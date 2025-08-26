using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;

[ServiceBinding(typeof(IKnowledgeService))]
public class KnowledgeService(IIndustryRepository industryRepository, ICharacterRepository characterRepository, IIndustryMembershipService membershipService) : IKnowledgeService
{
    public bool AddKnowledge(Guid characterGuid, string tag)
    {
        return false;
    }

    public List<CharacterKnowledge> GetKnowledge(Guid characterGuid)
    {
        throw new NotImplementedException();
    }

    public bool RemoveKnowledge(Guid characterGuid, string tag)
    {
        throw new NotImplementedException();
    }

    public bool HasKnowledge(Guid characterGuid, string tag)
    {
        throw new NotImplementedException();
    }

    public List<string> GetTags(Guid characterGuid)
    {
        throw new NotImplementedException();
    }
}
