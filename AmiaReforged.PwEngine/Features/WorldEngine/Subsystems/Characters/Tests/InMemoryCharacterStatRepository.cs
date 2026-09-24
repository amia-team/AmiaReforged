using System;
using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Tests;

/// <summary>
/// In-memory test double for <see cref="ICharacterStatRepository"/> that records
/// save calls and tracks statistics by character id. Requires no live NWN
/// objects or database.
/// </summary>
public sealed class InMemoryCharacterStatRepository : ICharacterStatRepository
{
    private readonly Dictionary<Guid, CharacterStatistics> _statistics = new();

    public int SaveCount { get; private set; }

    public void Seed(CharacterStatistics statistics)
    {
        _statistics[statistics.CharacterId] = statistics;
    }

    public CharacterStatistics? GetCharacterStatistics(Guid characterId)
    {
        return _statistics.GetValueOrDefault(characterId);
    }

    public void UpdateCharacterStatistics(CharacterStatistics statistics)
    {
        _statistics[statistics.CharacterId] = statistics;
    }

    public void SaveChanges()
    {
        SaveCount++;
    }
}
