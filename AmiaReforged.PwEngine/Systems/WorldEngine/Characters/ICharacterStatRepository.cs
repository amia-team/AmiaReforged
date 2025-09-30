using AmiaReforged.PwEngine.Systems.WorldEngine.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

public interface ICharacterStatRepository
{
    CharacterStatistics? GetCharacterStatistics(Guid characterId);
    void UpdateCharacterStatistics(CharacterStatistics statistics);
    void SaveChanges();
}
