using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using Anvil.Services;

namespace AmiaReforged.PwEngine;

[ServiceBinding(typeof(PersistentKnowledgeMapper))]
public class PersistentKnowledgeMapper(IIndustryRepository industries)
{
    public PersistentCharacterKnowledge ToPersistent(CharacterKnowledge characterKnowledge)
    {
        return new PersistentCharacterKnowledge
        {
            Id = characterKnowledge.Id,
            IndustryTag = characterKnowledge.IndustryTag,
            KnowledgeTag = characterKnowledge.Definition.Tag,
            CharacterId = characterKnowledge.CharacterId
        };
    }

    public CharacterKnowledge? ToDomain(PersistentCharacterKnowledge persistentKnowledge)
    {
        Industry? industry = industries.Get(persistentKnowledge.IndustryTag);

        Knowledge? knowledge = industry?.Knowledge.FirstOrDefault(k => k.Tag == persistentKnowledge.KnowledgeTag);

        if (knowledge is null) return null;

        return new CharacterKnowledge
        {
            CharacterId = persistentKnowledge.CharacterId,
            Definition = knowledge,
            Id = persistentKnowledge.Id,
            IndustryTag = persistentKnowledge.IndustryTag
        };
    }
}
