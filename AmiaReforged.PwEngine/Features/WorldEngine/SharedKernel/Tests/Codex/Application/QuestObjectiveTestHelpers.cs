using System.Threading.Channels;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application;

/// <summary>
/// Test helpers that replicate <see cref="QuestObjectiveResolutionService"/> signal-routing
/// and session-creation logic without requiring NWN module references.
/// </summary>
internal static class QuestObjectiveTestHelpers
{
    /// <summary>
    /// Processes an item-acquired signal through the session manager and writes
    /// any resulting domain events into the channel.
    /// </summary>
    public static void ProcessItemAcquired(
        QuestSessionManager sessionManager,
        Channel<CodexDomainEvent> eventChannel,
        CharacterId characterId,
        string itemTag)
    {
        QuestSignal signal = new(SignalType.ItemAcquired, itemTag);
        RouteSignal(sessionManager, eventChannel, characterId, signal);
    }

    /// <summary>
    /// Processes an item-lost signal through the session manager and writes
    /// any resulting domain events into the channel.
    /// </summary>
    public static void ProcessItemLost(
        QuestSessionManager sessionManager,
        Channel<CodexDomainEvent> eventChannel,
        CharacterId characterId,
        string itemTag)
    {
        QuestSignal signal = new(SignalType.ItemLost, itemTag);
        RouteSignal(sessionManager, eventChannel, characterId, signal);
    }

    /// <summary>
    /// Processes a dialogue-node-entered signal through the session manager,
    /// using the truncated node ID as the target tag (same as production
    /// <c>ProcessDialogueNodeEntered</c>).
    /// </summary>
    public static void ProcessDialogueNodeEntered(
        QuestSessionManager sessionManager,
        Channel<CodexDomainEvent> eventChannel,
        CharacterId characterId,
        DialogueNodeId nodeId)
    {
        string shortNodeId = nodeId.ToShortString();
        QuestSignal signal = new(SignalType.DialogChoice, shortNodeId);
        RouteSignal(sessionManager, eventChannel, characterId, signal);
    }

    /// <summary>
    /// Creates a quest session from a <see cref="CodexQuestEntry"/>, extracting
    /// the objective groups from the current stage.
    /// </summary>
    public static void CreateSessionForQuest(
        QuestSessionManager sessionManager,
        CharacterId characterId,
        CodexQuestEntry quest)
    {
        List<QuestObjectiveGroup> objectiveGroups = GetCurrentStageObjectiveGroups(quest);
        if (objectiveGroups.Count == 0) return;

        StageContext? stageContext = quest.Stages.Count > 0
            ? new StageContext(quest.Stages, quest.CurrentStageId)
            : null;

        sessionManager.CreateSession(characterId, quest.QuestId, objectiveGroups, stageContext: stageContext);
    }

    /// <summary>
    /// Extracts the objective groups for the quest entry's current stage.
    /// Returns the objectives from the highest stage ≤ <see cref="CodexQuestEntry.CurrentStageId"/>.
    /// </summary>
    public static List<QuestObjectiveGroup> GetCurrentStageObjectiveGroups(CodexQuestEntry quest)
    {
        if (quest.Stages.Count == 0) return [];

        QuestStage? currentStage = quest.Stages
            .Where(s => s.StageId <= quest.CurrentStageId)
            .OrderByDescending(s => s.StageId)
            .FirstOrDefault();

        return currentStage?.ObjectiveGroups ?? [];
    }

    private static void RouteSignal(
        QuestSessionManager sessionManager,
        Channel<CodexDomainEvent> eventChannel,
        CharacterId characterId,
        QuestSignal signal)
    {
        IReadOnlyList<CodexDomainEvent> events = sessionManager.ProcessSignal(characterId, signal);
        if (events.Count == 0) return;

        foreach (CodexDomainEvent domainEvent in events)
        {
            eventChannel.Writer.TryWrite(domainEvent);
        }
    }
}
