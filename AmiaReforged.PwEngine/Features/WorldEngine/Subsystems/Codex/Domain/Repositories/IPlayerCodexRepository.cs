using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;

/// <summary>
/// Repository interface for PlayerCodex persistence.
/// Implementations handle storage concerns (in-memory, JSON, database, etc.)
/// </summary>
public interface IPlayerCodexRepository
{
    /// <summary>
    /// Loads a player's codex by character ID.
    /// </summary>
    /// <param name="characterId">The character ID to load the codex for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The player's codex, or null if not found</returns>
    Task<PlayerCodex?> LoadAsync(CharacterId characterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the codex for a command supplying an explicit quest stage. A missing entry for
    /// that quest is left for the command to instantiate, so its explicit stage wins over the default.
    /// </summary>
    Task<PlayerCodex?> LoadAsync(CharacterId characterId, QuestId? explicitlyStagedQuestId, CancellationToken cancellationToken)
        => LoadAsync(characterId, cancellationToken);

    /// <summary>
    /// Saves a player's codex.
    /// </summary>
    /// <param name="codex">The codex to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveAsync(PlayerCodex codex, CancellationToken cancellationToken = default);
}
