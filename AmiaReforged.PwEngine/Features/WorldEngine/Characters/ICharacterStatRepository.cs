using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters;

public interface ICharacterStatRepository
{
    CharacterStatistics? GetCharacterStatistics(Guid characterId);
    void UpdateCharacterStatistics(CharacterStatistics statistics);
    void SaveChanges();
}
