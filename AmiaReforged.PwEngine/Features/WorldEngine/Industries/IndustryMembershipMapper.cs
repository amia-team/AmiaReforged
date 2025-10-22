using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Industries;

/// <summary>
/// Maps from a persistent instance to a domain-centric instance, and vice versa
/// </summary>
[ServiceBinding(typeof(IndustryMembershipMapper))]
public class IndustryMembershipMapper(PersistentKnowledgeMapper knowledgeMapper, IIndustryRepository industries)
{
    public PersistentIndustryMembership ToPersistent(IndustryMembership membership)
    {
        List<PersistentCharacterKnowledge> knowledge = [];
        knowledge.AddRange(membership.CharacterKnowledge.Select(knowledgeMapper.ToPersistent));

        return new PersistentIndustryMembership
        {
            Id = membership.Id,
            CharacterId = membership.CharacterId,
            IndustryTag = membership.IndustryTag,
            Level = ProficiencyLevel.Novice,
            Knowledge = knowledge
        };
    }

    /// <summary>
    /// Searches the existing industries and attempts to map the fields of a persistent object back to the domain.
    /// </summary>
    /// <param name="membership"></param>
    /// <returns>either a domain representation of a membership or a null value if the industry is not found</returns>
    public IndustryMembership? ToDomain(PersistentIndustryMembership membership)
    {
        Industry? industry = industries.Get(membership.IndustryTag);
        if (industry is null) return null;

        List<CharacterKnowledge> characterKnowledge = [];
        characterKnowledge.AddRange(membership.Knowledge
            .Select(knowledgeMapper.ToDomain).OfType<CharacterKnowledge>());

        return new IndustryMembership
        {
            CharacterId = CharacterId.From(membership.CharacterId),
            IndustryTag = new IndustryTag(membership.IndustryTag),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = characterKnowledge
        };
    }
}
