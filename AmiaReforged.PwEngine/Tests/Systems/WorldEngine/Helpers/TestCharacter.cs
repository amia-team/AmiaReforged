using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items.ItemData;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers;

/// <summary>
/// For use in testing only.
/// </summary>
/// <param name="injectedEquipment"></param>
/// <param name="skills"></param>
/// <param name="inventory"></param>
public class TestCharacter(
    Dictionary<EquipmentSlots, ItemSnapshot> injectedEquipment,
    List<SkillData> skills,
    Guid id,
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

    public Guid GetId()
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
        if (AllIndustryMemberships().Any(m => m.IndustryTag == industryTag)) return;

        IndustryMembership m = new()
        {
            CharacterId = GetId(),
            IndustryTag = industryTag,
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
        IndustryMembership? membership = AllIndustryMemberships().FirstOrDefault(i => i.IndustryTag == industryTag);
        return membership == null ? RankUpResult.IndustryNotFound : membershipService.RankUp(membership);
    }
}
