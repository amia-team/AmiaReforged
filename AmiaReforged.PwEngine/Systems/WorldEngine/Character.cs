using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

/// <summary>
/// Runtime specific implementation for a character
/// </summary>
public class RuntimeCharacter(
    Guid characterId,
    IInventoryPort inventoryPort,
    ICharacterSheetPort characterSheetPort,
    IIndustryMembershipService membershipService) : ICharacter
{
    private readonly Dictionary<string, List<KnowledgeHarvestEffect>> _nodeEffectCache = new();

    public int GetKnowledgePoints()
    {
        return 0;
    }

    public void SubtractKnowledgePoints(int points)
    {
        // TODO: Subtract knowledge...
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
        throw new NotImplementedException();
    }

    public List<KnowledgeHarvestEffect> KnowledgeEffectsForResource(string definitionTag)
    {
        throw new NotImplementedException();
    }

    public void AddItem(ItemDto item)
    {
        throw new NotImplementedException();
    }

    public List<ItemSnapshot> GetInventory()
    {
        throw new NotImplementedException();
    }

    public Dictionary<EquipmentSlots, ItemSnapshot> GetEquipment()
    {
        throw new NotImplementedException();
    }

    public void JoinIndustry(string industryTag)
    {
        IndustryMembership m = new IndustryMembership
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
        throw new NotImplementedException();
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
