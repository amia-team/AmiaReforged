using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters;

public interface ICharacter : ICharacterKnowledgeContext, ICharacterInventoryContext, ICharacterIndustryContext
{
    Guid GetId();
    List<SkillData> GetSkills();
}
