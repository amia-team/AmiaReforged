using System.Collections.Concurrent;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// In-memory implementation of <see cref="IInteractionSessionManager"/>.
/// Thread-safe via <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
[ServiceBinding(typeof(IInteractionSessionManager))]
public sealed class InteractionSessionManager : IInteractionSessionManager
{
    private readonly ConcurrentDictionary<CharacterId, InteractionSession> _sessions = new();

    /// <inheritdoc />
    public InteractionSession? GetActiveSession(CharacterId characterId)
        => _sessions.GetValueOrDefault(characterId);

    /// <inheritdoc />
    public bool HasActiveSession(CharacterId characterId)
        => _sessions.ContainsKey(characterId);

    /// <inheritdoc />
    public InteractionSession StartSession(
        CharacterId characterId,
        string interactionTag,
        Guid targetId,
        InteractionTargetMode targetMode,
        int requiredRounds,
        string? areaResRef = null,
        Dictionary<string, object>? metadata = null)
    {
        InteractionSession session = new()
        {
            CharacterId = characterId,
            InteractionTag = interactionTag,
            TargetId = targetId,
            TargetMode = targetMode,
            RequiredRounds = requiredRounds,
            AreaResRef = areaResRef,
            Metadata = metadata
        };

        _sessions[characterId] = session;
        return session;
    }

    /// <inheritdoc />
    public void EndSession(CharacterId characterId)
        => _sessions.TryRemove(characterId, out _);

    /// <inheritdoc />
    public IReadOnlyCollection<InteractionSession> GetAllSessions()
        => _sessions.Values.ToList();
}
