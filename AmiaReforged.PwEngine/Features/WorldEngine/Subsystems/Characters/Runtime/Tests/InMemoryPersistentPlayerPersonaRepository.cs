using System;
using System.Collections.Generic;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Tests;

/// <summary>
/// In-memory test double for <see cref="IPersistentPlayerPersonaRepository"/> that
/// records <see cref="Upsert"/> calls. Requires no live database.
/// </summary>
public sealed class InMemoryPersistentPlayerPersonaRepository : IPersistentPlayerPersonaRepository
{
    public List<(string CdKey, string DisplayName, DateTime? ObservedUtc)> UpsertCalls { get; } = new();

    public List<(string CdKey, DateTime ActivatedUtc)> TouchCalls { get; } = new();

    public PlayerPersonaRecord Upsert(string cdKey, string displayName, DateTime? observedUtc = null)
    {
        UpsertCalls.Add((cdKey, displayName, observedUtc));
        return new PlayerPersonaRecord { CdKey = cdKey, DisplayName = displayName };
    }

    public PlayerPersonaRecord? GetByCdKey(string cdKey) => null;

    public void Touch(string cdKey, DateTime observedUtc)
    {
        TouchCalls.Add((cdKey, observedUtc));
    }
}
