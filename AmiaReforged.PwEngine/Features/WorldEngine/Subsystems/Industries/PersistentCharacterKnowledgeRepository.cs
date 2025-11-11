using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.KnowledgeSubsystem;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

[ServiceBinding(typeof(ICharacterKnowledgeRepository))]
public class PersistentCharacterKnowledgeRepository(PersistentKnowledgeMapper mapper, PwContextFactory factory)
    : ICharacterKnowledgeRepository
{
    private readonly PwEngineContext _ctx = factory.CreateDbContext();

    public List<CharacterKnowledge> GetKnowledgeForIndustry(string industryTag, Guid characterId)
    {
        List<PersistentCharacterKnowledge> knowledge =
            _ctx.CharacterKnowledge.Where(x => x.CharacterId == characterId).ToList();

        List<CharacterKnowledge> domainKnowledge = [];
        domainKnowledge.AddRange(knowledge.Select(mapper.ToDomain).OfType<CharacterKnowledge>());


        return domainKnowledge;
    }

    public void Add(CharacterKnowledge ck)
    {
        PersistentCharacterKnowledge knowledgeEntity = mapper.ToPersistent(ck);

        _ctx.CharacterKnowledge.Add(knowledgeEntity);
    }

    public void SaveChanges()
    {
        _ctx.SaveChanges();
    }

    public bool AlreadyKnows(Guid membershipCharacterId, Knowledge tag)
    {
        bool exists =
            _ctx.CharacterKnowledge.Any(x => x.CharacterId == membershipCharacterId && x.KnowledgeTag == tag.Tag);
        return exists;
    }

    public List<Knowledge> GetAllKnowledge(Guid getId)
    {
        List<PersistentCharacterKnowledge> persistent =
            _ctx.CharacterKnowledge.Where(x => x.CharacterId == getId).ToList();

        List<Knowledge> domainKnowledge = [];
        domainKnowledge.AddRange(persistent.Select(mapper.ToDomain).OfType<CharacterKnowledge>()
            .Select(knowledge => knowledge.Definition));

        return domainKnowledge;
    }
}
