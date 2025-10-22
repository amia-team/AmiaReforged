using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters;

public interface ICharacter : ICharacterKnowledgeContext, ICharacterInventoryContext, ICharacterIndustryContext
{
    CharacterId GetId();
    List<SkillData> GetSkills();
}
