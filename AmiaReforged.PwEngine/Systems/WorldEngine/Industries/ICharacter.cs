using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public interface ICharacter : ICharacterKnowledgeContext, ICharacterInventoryContext, ICharacterIndustryContext
{
    Guid GetId();
    List<SkillData> GetSkills();
}

public interface ICharacterInventoryContext
{
    void AddItem(ItemDto item);
    List<ItemSnapshot> GetInventory();
    Dictionary<EquipmentSlots, ItemSnapshot> GetEquipment();
}

public interface ICharacterKnowledgeContext
{
    int GetKnowledgePoints();
    void AddKnowledgePoints(int points);
    void SubtractKnowledgePoints(int points);
    List<Knowledge> AllKnowledge();
    LearningResult Learn(string knowledgeTag);
    bool CanLearn(string knowledgeTag);
    List<KnowledgeHarvestEffect> KnowledgeEffectsForResource(string definitionTag);
}

public interface ICharacterIndustryContext
{
    void JoinIndustry(string industryTag);
    List<IndustryMembership> AllIndustryMemberships();
    RankUpResult RankUp(string industryTag);
}
