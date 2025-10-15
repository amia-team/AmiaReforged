using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters;

public interface ICharacterSheetPort
{
    List<SkillData> GetSkills();
}
