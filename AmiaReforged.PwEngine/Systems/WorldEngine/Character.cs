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
        throw new NotImplementedException();
    }

    public void SubtractKnowledgePoints(int points)
    {
        throw new NotImplementedException();
    }

    public List<Knowledge> AllKnowledge()
    {
        throw new NotImplementedException();
    }

    public LearningResult Learn(string knowledgeTag)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public List<IndustryMembership> AllIndustryMemberships()
    {
        throw new NotImplementedException();
    }

    public RankUpResult RankUp(string industryTag)
    {
        throw new NotImplementedException();
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
