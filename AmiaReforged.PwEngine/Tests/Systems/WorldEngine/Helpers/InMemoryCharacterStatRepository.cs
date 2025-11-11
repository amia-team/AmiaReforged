using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers;

public class InMemoryCharacterStatRepository : ICharacterStatRepository
{
    private Dictionary<Guid, CharacterStatistics> _stats = new();

    public CharacterStatistics? GetCharacterStatistics(Guid characterId)
    {
        CharacterStatistics? c = _stats.GetValueOrDefault(characterId);

        if (c != null) return c;

        _stats.TryAdd(characterId, new CharacterStatistics());
        c = _stats[characterId];

        return c;
    }

    public void UpdateCharacterStatistics(CharacterStatistics statistics)
    {
        CharacterStatistics? c = _stats.GetValueOrDefault(statistics.CharacterId);

        if (c == null)
        {
            _stats.TryAdd(statistics.CharacterId, new CharacterStatistics());
        }

        _stats[statistics.CharacterId] = statistics;
    }

    public void SaveChanges()
    {
        // nothing needed here
    }

    public static ICharacterStatRepository Create()
    {
        return new InMemoryCharacterStatRepository();
    }
}
