namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

public interface ICharacter : ICharacterKnowledgeContext, ICharacterInventoryContext, ICharacterIndustryContext
{
    Guid GetId();
    List<SkillData> GetSkills();
}