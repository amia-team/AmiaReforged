using AmiaReforged.PwEngine.Systems.WorldEngine.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

public interface ICharacterSheetPort
{
    List<SkillData> GetSkills();
}