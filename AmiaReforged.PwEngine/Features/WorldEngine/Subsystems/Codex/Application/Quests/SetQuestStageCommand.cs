using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Quests;

/// <summary>
/// Advances a quest to the given stage for a character.
/// If the quest is not yet in the character's codex, it is automatically
/// added from the quest definition and set to that stage.
/// Replaces the inline <c>CodexSubsystem.SetQuestStageAsync</c> path (F-6 audit).
/// Objective resolution stays handler-internal: after a successful advance the
/// handler starts objective tracking via <see cref="QuestObjectiveResolutionService"/>
/// (no separate signal command — resolution is always downstream of advancement).
/// </summary>
public record SetQuestStageCommand : ICommand
{
    public required CharacterId CharacterId { get; init; }
    public required string QuestId { get; init; }
    public required int StageId { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<SetQuestStageCommand>))]
public sealed class SetQuestStageHandler : ICommandHandler<SetQuestStageCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IPlayerCodexRepository _codexRepository;
    private readonly PwContextFactory _contextFactory;
    private readonly QuestObjectiveResolutionService _resolutionService;
    private readonly IStageRewardGranter? _rewardGranter;
    private readonly IEventBus _eventBus;

    public SetQuestStageHandler(
        IPlayerCodexRepository codexRepository,
        PwContextFactory contextFactory,
        QuestObjectiveResolutionService resolutionService,
        IEventBus eventBus,
        IStageRewardGranter? rewardGranter = null)
    {
        _codexRepository = codexRepository;
        _contextFactory = contextFactory;
        _resolutionService = resolutionService;
        _eventBus = eventBus;
        _rewardGranter = rewardGranter;
    }

    public async Task<CommandResult> HandleAsync(SetQuestStageCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            QuestId qid = (QuestId)command.QuestId;
            DateTime now = DateTime.UtcNow;

            // Load the player's codex (or create one if it doesn't exist)
            PlayerCodex? codex = await _codexRepository.LoadAsync(command.CharacterId, cancellationToken);
            codex ??= new PlayerCodex(command.CharacterId, now);

            // Track the from-stage so we can grant its rewards after advancing.
            int fromStageId = -1;
            CodexQuestEntry? questEntry;
            bool isNew;

            if (codex.HasQuest(qid))
            {
                isNew = false;

                // Always refresh stages from the definition so admin-panel edits
                // (new stages, updated journal text, etc.) reach existing player entries.
                CodexQuestEntry existing = codex.GetQuest(qid)!;
                using (PwEngineContext refreshCtx = _contextFactory.CreateDbContext())
                {
                    PersistedQuestDefinition? def = await refreshCtx.CodexQuestDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.QuestId == command.QuestId, cancellationToken);

                    if (def != null)
                    {
                        existing.Stages.Clear();
                        existing.Stages.AddRange(DeserializeStages(def.StagesJson));
                    }
                }

                fromStageId = existing.CurrentStageId;
                questEntry = existing;
                codex.AdvanceQuestStage(qid, command.StageId, now);
                ApplyStageQuestState(codex, existing, qid, command.StageId, now);
            }
            else
            {
                isNew = true;

                // Quest not in codex — look up the definition and add it
                using PwEngineContext ctx = _contextFactory.CreateDbContext();
                PersistedQuestDefinition? definition = await ctx.CodexQuestDefinitions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.QuestId == command.QuestId, cancellationToken);

                if (definition == null)
                    return CommandResult.Fail($"Quest definition '{command.QuestId}' not found");

                // Build a new codex entry from the definition
                CodexQuestEntry entry = new()
                {
                    QuestId = qid,
                    Title = definition.Title,
                    Description = definition.Description,
                    DateStarted = now,
                    QuestGiver = definition.QuestGiver,
                    Location = definition.Location,
                    Keywords = ParseKeywords(definition.Keywords),
                    Stages = DeserializeStages(definition.StagesJson)
                };

                fromStageId = 0;
                questEntry = entry;

                // Add to codex in InProgress state, then advance to the requested stage
                codex.RecordQuestStarted(entry, now);
                codex.AdvanceQuestStage(qid, command.StageId, now);
                ApplyStageQuestState(codex, entry, qid, command.StageId, now);
            }

            // Grant the completed (from) stage's rewards if any
            await GrantFromStageRewardsAsync(
                _rewardGranter, command.CharacterId, qid, fromStageId, command.StageId, questEntry, _eventBus, cancellationToken);

            await _codexRepository.SaveAsync(codex, cancellationToken);

            // Create/update the quest session so objective tracking begins immediately
            CodexQuestEntry? updatedEntry = codex.GetQuest(qid);
            if (updatedEntry is not null && updatedEntry.EffectiveState == QuestState.InProgress)
            {
                _resolutionService.CreateSessionForQuest(command.CharacterId, updatedEntry);
            }

            await PublishStageEventsAsync(
                command.CharacterId, qid, questEntry, isNew, fromStageId, command.StageId, now, cancellationToken);

            Log.Info("SetQuestStage: quest '{QuestId}' → stage {StageId} for character {CharacterId}",
                command.QuestId, command.StageId, command.CharacterId);
            return CommandResult.OkWith("questId", command.QuestId);
        }
        catch (InvalidOperationException ex)
        {
            Log.Warn(ex, "SetQuestStage domain error for quest '{QuestId}'", command.QuestId);
            return CommandResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SetQuestStage failed for quest '{QuestId}' character {CharacterId}",
                command.QuestId, command.CharacterId);
            return CommandResult.Fail($"Database error: {ex.Message}");
        }
    }

    private async Task PublishStageEventsAsync(
        CharacterId characterId,
        QuestId questId,
        CodexQuestEntry entry,
        bool isNew,
        int fromStageId,
        int toStageId,
        DateTime now,
        CancellationToken ct)
    {
        if (isNew)
        {
            await _eventBus.PublishAsync(
                new QuestStartedEvent(characterId, now, questId, entry.Title, entry.Description), ct);
        }
        else if (fromStageId != toStageId)
        {
            await _eventBus.PublishAsync(
                new QuestStageAdvancedEvent(characterId, now, questId, fromStageId, toStageId), ct);
        }

        switch (entry.EffectiveState)
        {
            case QuestState.Completed:
                await _eventBus.PublishAsync(new QuestCompletedEvent(characterId, now, questId), ct);
                break;
            case QuestState.Failed:
                await _eventBus.PublishAsync(
                    new QuestFailedEvent(characterId, now, questId, $"Stage {toStageId}"), ct);
                break;
            case QuestState.Abandoned:
                await _eventBus.PublishAsync(new QuestAbandonedEvent(characterId, now, questId), ct);
                break;
            case QuestState.Expired:
                await _eventBus.PublishAsync(
                    new QuestExpiredEvent(characterId, now, questId, ExpiryBehavior.Fail), ct);
                break;
        }
    }

    /// <summary>
    /// Applies the target stage's <see cref="QuestState"/> to the codex entry, transitioning
    /// to Completed/Failed/etc. if the stage defines it. Ensures the entry-level State stays
    /// in sync with stage-level overrides at write time.
    /// </summary>
    private static void ApplyStageQuestState(
        PlayerCodex codex, CodexQuestEntry entry, QuestId qid, int stageId, DateTime now)
    {
        QuestStage? targetStage = entry.Stages.FirstOrDefault(s => s.StageId == stageId);
        if (targetStage?.QuestState is not { } stageState)
        {
            // Backward compat: IsCompletionStage still works if QuestState is not set
            if (targetStage is { IsCompletionStage: true })
                codex.RecordQuestCompleted(qid, now);
            return;
        }

        switch (stageState)
        {
            case QuestState.Completed:
                codex.RecordQuestCompleted(qid, now);
                break;
            case QuestState.Failed:
                codex.RecordQuestFailed(qid, now);
                break;
            case QuestState.Abandoned:
                codex.RecordQuestAbandoned(qid, now);
                break;
            case QuestState.Expired:
                codex.RecordQuestExpired(qid, ExpiryBehavior.Fail, now);
                break;
            default:
                entry.State = stageState;
                break;
        }
    }

    /// <summary>
    /// Grants the FROM stage's rewards (if any) when advancing from one stage to another.
    /// Skipped when the stage didn't actually change (idempotent advance) or when no
    /// reward granter is registered. Publishes <see cref="StageRewardsGrantedEvent"/>
    /// when rewards are granted.
    /// </summary>
    internal static async Task GrantFromStageRewardsAsync(
        IStageRewardGranter? rewardGranter,
        CharacterId characterId,
        QuestId questId,
        int fromStageId,
        int toStageId,
        CodexQuestEntry entry,
        IEventBus? eventBus,
        CancellationToken ct)
    {
        // Idempotent: stage didn't change — nothing to grant
        if (fromStageId == toStageId) return;

        if (rewardGranter is null) return;

        QuestStage? fromStage = entry.Stages.FirstOrDefault(s => s.StageId == fromStageId);
        if (fromStage is null or { Rewards.IsEmpty: true }) return;

        try
        {
            await rewardGranter.GrantRewardsAsync(characterId, questId, fromStageId, fromStage.Rewards);
            if (eventBus is not null)
            {
                await eventBus.PublishAsync(
                    new StageRewardsGrantedEvent(characterId, DateTime.UtcNow, questId, fromStageId, fromStage.Rewards), ct);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Failed to grant stage {StageId} rewards for quest '{QuestId}' character {CharacterId}",
                fromStageId, questId.Value, characterId.Value);
        }
    }

    private static List<Keyword> ParseKeywords(string? keywords)
    {
        if (string.IsNullOrWhiteSpace(keywords)) return [];
        return keywords
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(k => new Keyword(k))
            .ToList();
    }

    private static readonly JsonSerializerOptions StageJsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new ObjectiveIdJsonConverter()
        },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static List<QuestStage> DeserializeStages(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json is "[]" or "null")
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<QuestStage>>(json, StageJsonOpts) ?? [];
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to deserialize quest stages JSON from definition");
            return [];
        }
    }
}
