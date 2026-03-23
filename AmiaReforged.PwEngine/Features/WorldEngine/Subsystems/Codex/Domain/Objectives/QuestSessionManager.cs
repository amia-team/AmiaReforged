using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;

/// <summary>
/// Manages active quest sessions per character.
/// Provides signal routing and session lifecycle management.
/// </summary>
public class QuestSessionManager
{
    private readonly IObjectiveEvaluatorRegistry _registry;

    // CharacterId → QuestId → QuestSession
    private readonly Dictionary<CharacterId, Dictionary<QuestId, QuestSession>> _sessions = new();

    public QuestSessionManager(IObjectiveEvaluatorRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Creates a new quest session for a character.
    /// </summary>
    public QuestSession CreateSession(
        CharacterId characterId,
        QuestId questId,
        List<QuestObjectiveGroup> objectiveGroups,
        DateTime? createdAt = null)
    {
        QuestSession session = new(
            questId, characterId, objectiveGroups, _registry,
            createdAt ?? DateTime.UtcNow);

        if (!_sessions.ContainsKey(characterId))
            _sessions[characterId] = new Dictionary<QuestId, QuestSession>();

        _sessions[characterId][questId] = session;
        return session;
    }

    /// <summary>
    /// Gets the active session for a character and quest, or null if none exists.
    /// </summary>
    public QuestSession? GetSession(CharacterId characterId, QuestId questId)
    {
        if (_sessions.TryGetValue(characterId, out Dictionary<QuestId, QuestSession>? charSessions))
            return charSessions.GetValueOrDefault(questId);
        return null;
    }

    /// <summary>
    /// Checks if a character has an active session for the given quest.
    /// </summary>
    public bool HasSession(CharacterId characterId, QuestId questId)
        => GetSession(characterId, questId) != null;

    /// <summary>
    /// Processes a signal through ALL active sessions for a character.
    /// Returns the aggregated domain events from all sessions.
    /// </summary>
    public IReadOnlyList<CodexDomainEvent> ProcessSignal(CharacterId characterId, QuestSignal signal)
    {
        if (!_sessions.TryGetValue(characterId, out Dictionary<QuestId, QuestSession>? charSessions))
            return Array.Empty<CodexDomainEvent>();

        List<CodexDomainEvent> allEvents = [];

        foreach (QuestSession session in charSessions.Values)
        {
            IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(signal);
            allEvents.AddRange(events);
        }

        return allEvents;
    }

    /// <summary>
    /// Removes a quest session (e.g., when completed, failed, or abandoned).
    /// </summary>
    public bool RemoveSession(CharacterId characterId, QuestId questId)
    {
        if (!_sessions.TryGetValue(characterId, out Dictionary<QuestId, QuestSession>? charSessions))
            return false;

        bool removed = charSessions.Remove(questId);

        if (charSessions.Count == 0)
            _sessions.Remove(characterId);

        return removed;
    }

    /// <summary>
    /// Gets all active sessions for a character.
    /// </summary>
    public IReadOnlyCollection<QuestSession> GetAllSessions(CharacterId characterId)
    {
        if (_sessions.TryGetValue(characterId, out Dictionary<QuestId, QuestSession>? charSessions))
            return charSessions.Values;
        return Array.Empty<QuestSession>();
    }
}
