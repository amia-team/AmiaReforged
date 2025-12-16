using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;

/// <summary>
/// For use in testing only.
/// </summary>
/// <param name="injectedEquipment"></param>
/// <param name="skills"></param>
/// <param name="inventory"></param>
public class TestCharacter(
    Dictionary<EquipmentSlots, ItemSnapshot> injectedEquipment,
    List<SkillData> skills,
    CharacterId id,
    ICharacterKnowledgeRepository knowledgeRepository,
    IIndustryMembershipService membershipService,
    List<ItemSnapshot>? inventory = null,
    int knowledgePoints = 0)
    : ICharacter
{
    private readonly List<ItemSnapshot> _inventory = inventory ?? [];
    private int _knowledgePoints = knowledgePoints;

    public bool CanLearn(string knowledgeTag)
    {
        return true;
    }

    public List<KnowledgeHarvestEffect> KnowledgeEffectsForResource(string definitionTag)
    {
        return AllKnowledge()
            .SelectMany(knowledge => knowledge.HarvestEffects
                .Where(he => he.NodeTag == definitionTag))
            .ToList();
    }

    public int GetKnowledgePoints()
    {
        return _knowledgePoints;
    }

    public void SubtractKnowledgePoints(int points)
    {
        _knowledgePoints -= points;
    }

    public CharacterId GetId()
    {
        return id;
    }

    public void AddItem(ItemDto item) =>
        _inventory.Add(new ItemSnapshot(item.BaseDefinition.ItemTag, item.BaseDefinition.Name,
            item.BaseDefinition.Description, item.Quality, item.BaseDefinition.Materials,
            item.BaseDefinition.JobSystemType, item.BaseDefinition.BaseItemType, null));

    public List<ItemSnapshot> GetInventory() => _inventory;

    public List<Knowledge> AllKnowledge()
    {
        return knowledgeRepository.GetAllKnowledge(GetId());
    }

    public LearningResult Learn(string knowledgeTag)
    {
        return membershipService.LearnKnowledge(GetId(), knowledgeTag);
    }

    public Dictionary<EquipmentSlots, ItemSnapshot?> GetEquipment() => injectedEquipment;

    public List<SkillData> GetSkills()
    {
        return skills;
    }

    public void JoinIndustry(string industryTag)
    {
        IndustryTag tag = new(industryTag);
        if (AllIndustryMemberships().Any(m => m.IndustryTag.Value == tag.Value)) return;

        IndustryMembership m = new()
        {
            CharacterId = GetId(),
            IndustryTag = tag,
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = []
        };

        membershipService.AddMembership(m);
    }

    public void AddKnowledgePoints(int points)
    {
        _knowledgePoints += points;
    }

    public List<IndustryMembership> AllIndustryMemberships()
    {
        return membershipService.GetMemberships(GetId());
    }

    public RankUpResult RankUp(string industryTag)
    {
        IndustryTag tag = new(industryTag);
        IndustryMembership? membership = AllIndustryMemberships().FirstOrDefault(i => i.IndustryTag.Value == tag.Value);
        return membership == null ? RankUpResult.IndustryNotFound : membershipService.RankUp(membership);
    }

}
