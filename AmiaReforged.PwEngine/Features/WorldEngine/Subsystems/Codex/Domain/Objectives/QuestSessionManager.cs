using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;

/// <summary>
/// Manages active quest sessions per character.
/// Provides signal routing and session lifecycle management.
/// </summary>
[ServiceBinding(typeof(QuestSessionManager))]
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

    #region Dynamic Quest Extensions

    /// <summary>
    /// Creates a shared quest session registered under multiple characters.
    /// All party members' signals route to the same session, enabling co-op objective progress.
    /// </summary>
    public QuestSession CreateSharedSession(
        QuestId questId,
        CharacterId primaryCharacterId,
        List<CharacterId> partyMembers,
        List<QuestObjectiveGroup> objectiveGroups,
        DateTime? deadline = null,
        DateTime? createdAt = null)
    {
        DateTime created = createdAt ?? DateTime.UtcNow;

        QuestSession session = new(
            questId, primaryCharacterId, objectiveGroups, _registry,
            created, deadline, partyMembers);

        // Register the session under every party member so their signals route to it
        foreach (CharacterId memberId in session.PartyMembers)
        {
            if (!_sessions.ContainsKey(memberId))
                _sessions[memberId] = new Dictionary<QuestId, QuestSession>();

            _sessions[memberId][questId] = session;
        }

        return session;
    }

    /// <summary>
    /// Adds a new member to an existing shared session.
    /// The session is registered under the new member's character ID for signal routing.
    /// </summary>
    public void AddToSession(QuestId questId, CharacterId existingMember, CharacterId newMember)
    {
        QuestSession? session = GetSession(existingMember, questId)
            ?? throw new InvalidOperationException(
                $"No active session found for quest {questId} and character {existingMember.Value}");

        if (session.PartyMembers.Contains(newMember))
            throw new InvalidOperationException(
                $"Character {newMember.Value} is already in the session for quest {questId}");

        session.PartyMembers.Add(newMember);

        if (!_sessions.ContainsKey(newMember))
            _sessions[newMember] = new Dictionary<QuestId, QuestSession>();

        _sessions[newMember][questId] = session;
    }

    /// <summary>
    /// Removes a member from a shared session.
    /// Unregisters the session from their character ID but does not destroy the session
    /// unless they were the last member.
    /// </summary>
    public void RemoveFromSession(QuestId questId, CharacterId memberId)
    {
        QuestSession? session = GetSession(memberId, questId);
        if (session == null) return;

        session.PartyMembers.Remove(memberId);

        if (_sessions.TryGetValue(memberId, out Dictionary<QuestId, QuestSession>? charSessions))
        {
            charSessions.Remove(questId);
            if (charSessions.Count == 0)
                _sessions.Remove(memberId);
        }

        // If no members remain, clean up the session entirely
        if (session.PartyMembers.Count == 0)
        {
            // Session is orphaned — nothing to clean up in _sessions since all members were removed
        }
    }

    /// <summary>
    /// Checks all active sessions for expired deadlines and emits <see cref="QuestExpiredEvent"/>s.
    /// Returns the list of expiration events. The caller is responsible for enqueuing them
    /// into the event processor and removing the expired sessions.
    /// </summary>
    public IReadOnlyList<CodexDomainEvent> TickDeadlines(DateTime now)
    {
        List<CodexDomainEvent> expirationEvents = [];
        List<(CharacterId, QuestId)> toRemove = [];

        // Collect unique sessions (shared sessions are registered under multiple characters)
        HashSet<QuestSession> visitedSessions = new(ReferenceEqualityComparer.Instance);

        foreach ((CharacterId charId, Dictionary<QuestId, QuestSession> charSessions) in _sessions)
        {
            foreach ((QuestId questId, QuestSession session) in charSessions)
            {
                if (!visitedSessions.Add(session))
                    continue; // Already processed this shared session

                if (!session.Deadline.HasValue || now < session.Deadline.Value)
                    continue; // No deadline or not yet expired

                // Emit expiration events for all party members
                foreach (CharacterId memberId in session.PartyMembers)
                {
                    expirationEvents.Add(new QuestExpiredEvent(
                        memberId, now, session.QuestId,
                        ExpiryBehavior.Fail)); // Default; actual behavior resolved by application layer

                    toRemove.Add((memberId, session.QuestId));
                }
            }
        }

        // Remove expired sessions
        foreach ((CharacterId charId, QuestId questId) in toRemove)
        {
            RemoveSession(charId, questId);
        }

        return expirationEvents;
    }

    #endregion
}
