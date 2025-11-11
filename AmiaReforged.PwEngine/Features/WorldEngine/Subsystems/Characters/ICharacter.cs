using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

public interface ICharacter : ICharacterKnowledgeContext, ICharacterInventoryContext, ICharacterIndustryContext
{
    CharacterId GetId();
    List<SkillData> GetSkills();
}
