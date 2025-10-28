using System.Collections.Concurrent;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Codex.Infrastructure;

/// <summary>
/// In-memory implementation of IPlayerCodexRepository for testing.
/// NOT intended for production use - use a persistent implementation instead.
/// </summary>
public class InMemoryPlayerCodexRepository : IPlayerCodexRepository
{
    private readonly ConcurrentDictionary<CharacterId, PlayerCodex> _storage = new();

    /// <summary>
    /// Loads a player's codex from memory.
    /// </summary>
    public Task<PlayerCodex?> LoadAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(characterId, out PlayerCodex? codex);
        return Task.FromResult(codex);
    }

    /// <summary>
    /// Saves a player's codex to memory.
    /// </summary>
    public Task SaveAsync(PlayerCodex codex, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(codex);
        _storage[codex.OwnerId] = codex;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all stored codices. For testing only.
    /// </summary>
    public void Clear() => _storage.Clear();

    /// <summary>
    /// Gets the number of stored codices. For testing only.
    /// </summary>
    public int Count => _storage.Count;

    /// <summary>
    /// Gets all stored codices. For testing only.
    /// </summary>
    public IEnumerable<PlayerCodex> GetAll() => _storage.Values;
}

