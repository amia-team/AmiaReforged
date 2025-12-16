using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;

public class InMemoryCharacterKnowledgeRepository : ICharacterKnowledgeRepository
{
    private readonly List<CharacterKnowledge> _characterKnowledge = [];

    public List<CharacterKnowledge> GetKnowledgeForIndustry(string industryTag, Guid characterId)
    {
        return _characterKnowledge.Where(ck => ck.CharacterId == characterId && ck.IndustryTag == industryTag).ToList();
    }

    public List<Knowledge> GetAllKnowledge(Guid getId)
    {
        return _characterKnowledge.Where(ck => ck.CharacterId == getId).Select(ck => ck.Definition).ToList();
    }

    public void Add(CharacterKnowledge ck)
    {
        if (_characterKnowledge.Any(c =>
                c.CharacterId == ck.CharacterId && c.Definition.Tag == ck.Definition.Tag)) return;

        _characterKnowledge.Add(ck);
    }

    public void SaveChanges()
    {
        // nothing
    }

    public bool AlreadyKnows(Guid characterId, Knowledge knowledge)
    {
        return _characterKnowledge.Any(ck => ck.CharacterId == characterId && ck.Definition.Tag == knowledge.Tag);
    }

    public static ICharacterKnowledgeRepository Create()
    {
        return new InMemoryCharacterKnowledgeRepository();
    }
}
