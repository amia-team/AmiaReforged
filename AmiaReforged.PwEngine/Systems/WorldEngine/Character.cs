using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

/// <summary>
/// Runtime specific implementation for a character. Acts as a facade for various character services...
/// </summary>
public class RuntimeCharacter(
    Guid characterId,
    IInventoryPort inventoryPort,
    ICharacterSheetPort characterSheetPort,
    IIndustryMembershipService membershipService,
    ICharacterStatService statService) : ICharacter
{
    private readonly Dictionary<string, List<KnowledgeHarvestEffect>> _nodeEffectCache = new();

    public int GetKnowledgePoints()
    {
        return statService.GetKnowledgePoints(characterId);
    }

    public void SubtractKnowledgePoints(int points)
    {
        int newPoints = GetKnowledgePoints() - points;
        statService.UpdateKnowledgePoints(characterId, newPoints);
    }

    public void AddKnowledgePoints(int points)
    {
        int newPoints = GetKnowledgePoints() + points;
        statService.UpdateKnowledgePoints(characterId, newPoints);
    }

    public List<Knowledge> AllKnowledge()
    {
        return membershipService.AllKnowledge(characterId);
    }

    public LearningResult Learn(string knowledgeTag)
    {
        return membershipService.LearnKnowledge(characterId, knowledgeTag);
    }

    public bool CanLearn(string knowledgeTag)
    {
        return membershipService.CanLearnKnowledge(characterId, knowledgeTag);
    }

    public List<KnowledgeHarvestEffect> KnowledgeEffectsForResource(string definitionTag)
    {
        if (_nodeEffectCache.TryGetValue(definitionTag, out List<KnowledgeHarvestEffect>? effects))
        {
            return effects;
        }

        List<KnowledgeHarvestEffect> knowledgeEffectsForResource = AllKnowledge()
            .SelectMany(knowledge => knowledge.HarvestEffects
                .Where(he => he.NodeTag == definitionTag))
            .ToList();

        _nodeEffectCache.TryAdd(definitionTag, knowledgeEffectsForResource);

        return knowledgeEffectsForResource;
    }

    public void AddItem(ItemDto item)
    {
        inventoryPort.AddItem(item);
    }

    public List<ItemSnapshot> GetInventory()
    {
        return inventoryPort.GetInventory();
    }

    public Dictionary<EquipmentSlots, ItemSnapshot> GetEquipment()
    {
        return inventoryPort.GetEquipment();
    }

    public void JoinIndustry(string industryTag)
    {
        IndustryMembership m = new()
        {
            IndustryTag = industryTag,
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            CharacterId = characterId
        };

        membershipService.AddMembership(m);
    }

    public List<IndustryMembership> AllIndustryMemberships()
    {
        return membershipService.GetMemberships(characterId);
    }

    public RankUpResult RankUp(string industryTag)
    {
        return membershipService.RankUp(characterId, industryTag);
    }

    public Guid GetId()
    {
        return characterId;
    }

    public List<SkillData> GetSkills()
    {
        return characterSheetPort.GetSkills();
    }
}

public interface ICharacterSheetPort
{
    List<SkillData> GetSkills();
}

public interface IInventoryPort
{
    void AddItem(ItemDto item);
    List<ItemSnapshot> GetInventory();
    Dictionary<EquipmentSlots, ItemSnapshot> GetEquipment();
}
