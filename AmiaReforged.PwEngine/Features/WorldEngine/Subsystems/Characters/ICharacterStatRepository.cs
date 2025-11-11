using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

public interface ICharacterStatRepository
{
    CharacterStatistics? GetCharacterStatistics(Guid characterId);
    void UpdateCharacterStatistics(CharacterStatistics statistics);
    void SaveChanges();
}
