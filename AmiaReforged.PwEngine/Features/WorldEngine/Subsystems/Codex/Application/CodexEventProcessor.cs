using System.Threading.Channels;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;

/// <summary>
/// Processes codex events from a channel and applies them to PlayerCodex aggregates.
/// Ensures sequential processing per character to maintain consistency.
/// </summary>
[ServiceBinding(typeof(CodexEventProcessor))]
public class CodexEventProcessor
{
    private readonly IPlayerCodexRepository _repository;
    private readonly ITraitSubsystem? _traitSubsystem;
    private readonly Channel<CodexDomainEvent> _eventChannel;
    private readonly CancellationTokenSource _cts;
    private Task? _processingTask;

    public CodexEventProcessor(IPlayerCodexRepository repository, ITraitSubsystem? traitSubsystem = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _traitSubsystem = traitSubsystem;
        _eventChannel = Channel.CreateUnbounded<CodexDomainEvent>();
        _cts = new CancellationTokenSource();
        Start();
    }

    /// <summary>
    /// Internal constructor for testing that allows injecting a custom channel
    /// </summary>
    internal CodexEventProcessor(IPlayerCodexRepository repository, Channel<CodexDomainEvent> channel, ITraitSubsystem? traitSubsystem = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _traitSubsystem = traitSubsystem;
        _eventChannel = channel ?? throw new ArgumentNullException(nameof(channel));
        _cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Starts the event processor. Safe to call multiple times — subsequent calls are no-ops.
    /// </summary>
    public void Start()
    {
        if (_processingTask != null)
            return;

        _processingTask = Task.Run(() => ProcessEventsAsync(_cts.Token), _cts.Token);
    }

    /// <summary>
    /// Stops the event processor
    /// </summary>
    public async Task StopAsync()
    {
        if (_processingTask == null)
            return;

        _cts.Cancel();

        try
        {
            await _processingTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }

        _processingTask = null;
    }

    /// <summary>
    /// Enqueues an event for processing
    /// </summary>
    public async Task EnqueueEventAsync(CodexDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        await _eventChannel.Writer.WriteAsync(domainEvent, cancellationToken);
    }

    /// <summary>
    /// Main event processing loop
    /// </summary>
    private async Task ProcessEventsAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement per-character sequential processing using GroupBy or similar
        // For now, simple sequential processing

        await foreach (CodexDomainEvent domainEvent in _eventChannel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await ProcessSingleEventAsync(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                // TODO: Add proper logging
                Console.WriteLine($"Error processing event: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Processes a single event
    /// </summary>
    private async Task ProcessSingleEventAsync(CodexDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Load codex (or create if first event)
        PlayerCodex codex = await _repository.LoadAsync(domainEvent.CharacterId, cancellationToken)
                            ?? new PlayerCodex(domainEvent.CharacterId, domainEvent.OccurredAt);

        // Apply event to aggregate
        await ApplyEventAsync(codex, domainEvent, cancellationToken);

        // Save updated codex
        await _repository.SaveAsync(codex, cancellationToken);
    }

    /// <summary>
    /// Applies a domain event to the PlayerCodex aggregate.
    /// Async because trait resolution requires a subsystem lookup.
    /// </summary>
    private async Task ApplyEventAsync(PlayerCodex codex, CodexDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case QuestDiscoveredEvent qde:
                CodexQuestEntry discoveredQuest = CreateQuestEntry(qde.QuestId, qde.QuestName, qde.Description, qde.OccurredAt);
                codex.RecordQuestDiscovered(discoveredQuest, qde.OccurredAt);
                break;

            case QuestStartedEvent qse:
                CodexQuestEntry questEntry = CreateQuestEntry(qse);
                codex.RecordQuestStarted(questEntry, qse.OccurredAt);
                break;

            case QuestCompletedEvent qce:
                codex.RecordQuestCompleted(qce.QuestId, qce.OccurredAt);
                break;

            case QuestFailedEvent qfe:
                codex.RecordQuestFailed(qfe.QuestId, qfe.OccurredAt);
                break;

            case QuestAbandonedEvent qae:
                codex.RecordQuestAbandoned(qae.QuestId, qae.OccurredAt);
                break;

            case LoreDiscoveredEvent lde:
                CodexLoreEntry loreEntry = CreateLoreEntry(lde);
                codex.RecordLoreDiscovered(loreEntry, lde.OccurredAt);
                break;

            case ReputationChangedEvent rce:
                // Note: Using FactionId.Value as faction name until event is updated
                codex.RecordReputationChange(rce.FactionId, rce.FactionId.Value, rce.Delta, rce.Reason, rce.OccurredAt);
                break;

            case NoteAddedEvent nae:
                CodexNoteEntry noteEntry = CreateNoteEntry(nae);
                codex.AddNote(noteEntry, nae.OccurredAt);
                break;

            case NoteEditedEvent nee:
                codex.EditNote(nee.NoteId, nee.NewContent, nee.OccurredAt);
                break;

            case NoteDeletedEvent nde:
                codex.DeleteNote(nde.NoteId, nde.OccurredAt);
                break;

            case TraitAcquiredEvent tae:
                CodexTraitEntry traitEntry = await CreateTraitEntryAsync(tae, cancellationToken);
                codex.RecordTraitAcquired(traitEntry, tae.OccurredAt);
                break;

            // --- Dynamic quest events ---

            case QuestClaimedEvent qcle:
                CodexQuestEntry claimedQuest = new()
                {
                    QuestId = qcle.QuestId,
                    Title = qcle.Title,
                    Description = qcle.Description,
                    DateStarted = qcle.OccurredAt,
                    SourceTemplateId = qcle.TemplateId,
                    Deadline = qcle.Deadline,
                    Keywords = new List<Keyword>()
                };
                codex.RecordQuestStarted(claimedQuest, qcle.OccurredAt);
                break;

            case QuestExpiredEvent qee:
                codex.RecordQuestExpired(qee.QuestId, qee.ExpiryBehavior, qee.OccurredAt);
                break;

            case QuestUnclaimedEvent que:
                codex.RemoveQuest(que.QuestId, que.OccurredAt);
                break;

            case QuestSharedEvent:
                // Sharing is handled at the session level by QuestSessionManager.
                // The codex event is recorded for audit/display purposes but does not
                // mutate the PlayerCodex aggregate — the invitee's codex entry is
                // created via a separate QuestClaimedEvent.
                break;

            // --- Objective tracking events (runtime state managed by QuestSession) ---

            case ObjectiveProgressedEvent:
            case ObjectiveCompletedEvent:
            case ObjectiveFailedEvent:
            case QuestObjectiveGroupCompletedEvent:
                // These events are emitted by QuestSession for observability / logging.
                // Runtime objective state lives in-memory on the session; the codex
                // does not persist per-objective progress. No mutation needed.
                break;

            case QuestStageAdvancedEvent qsae:
                codex.AdvanceQuestStage(qsae.QuestId, qsae.ToStage, qsae.OccurredAt);
                // If the new stage is a completion stage, mark the quest completed
                CodexQuestEntry? advancedQuest = codex.GetQuest(qsae.QuestId);
                QuestStage? targetStage = advancedQuest?.Stages
                    .FirstOrDefault(s => s.StageId == qsae.ToStage);
                if (targetStage is { IsCompletionStage: true })
                {
                    codex.RecordQuestCompleted(qsae.QuestId, qsae.OccurredAt);
                }
                break;

            default:
                throw new NotSupportedException($"Event type {domainEvent.GetType().Name} is not supported");
        }
    }

    /// <summary>
    /// Creates a CodexQuestEntry with common parameters
    /// </summary>
    private CodexQuestEntry CreateQuestEntry(QuestId questId, string questName, string description, DateTime occurredAt)
    {
        return new CodexQuestEntry
        {
            QuestId = questId,
            Title = questName,
            Description = description,
            DateStarted = occurredAt,
            Keywords = new List<Keyword>()
        };
    }

    /// <summary>
    /// Creates a CodexQuestEntry from a QuestStartedEvent
    /// </summary>
    private CodexQuestEntry CreateQuestEntry(QuestStartedEvent evt)
    {
        return new CodexQuestEntry
        {
            QuestId = evt.QuestId,
            Title = evt.QuestName,
            Description = evt.Description,
            State = QuestState.InProgress,
            DateStarted = evt.OccurredAt,
            Keywords = new List<Keyword>()
        };
    }

    /// <summary>
    /// Creates a CodexLoreEntry from a LoreDiscoveredEvent
    /// </summary>
    private CodexLoreEntry CreateLoreEntry(LoreDiscoveredEvent evt)
    {
        return new CodexLoreEntry
        {
            LoreId = evt.LoreId,
            Title = evt.Title,
            Content = evt.Summary,
            Category = evt.Category,
            Tier = evt.Tier,
            DateDiscovered = evt.OccurredAt,
            DiscoverySource = evt.Source,
            Keywords = evt.Keywords.ToList()
        };
    }

    /// <summary>
    /// Creates a CodexNoteEntry from a NoteAddedEvent
    /// </summary>
    private CodexNoteEntry CreateNoteEntry(NoteAddedEvent evt)
    {
        return new CodexNoteEntry(
            id: evt.NoteId,
            content: evt.Content,
            category: evt.Category,
            dateCreated: evt.OccurredAt,
            isDmNote: evt.IsDmNote,
            isPrivate: evt.IsPrivate,
            title: null
        );
    }

    /// <summary>
    /// Creates a CodexTraitEntry from a TraitAcquiredEvent by resolving metadata from the trait subsystem.
    /// Falls back to the TraitTag value as name if the subsystem is unavailable or the trait is unknown.
    /// </summary>
    private async Task<CodexTraitEntry> CreateTraitEntryAsync(TraitAcquiredEvent evt, CancellationToken cancellationToken)
    {
        string name = evt.TraitTag.Value;
        string description = string.Empty;
        TraitCategory category = TraitCategory.Background;

        if (_traitSubsystem != null)
        {
            TraitDefinition? definition = await _traitSubsystem.GetTraitAsync(evt.TraitTag, cancellationToken);
            if (definition != null)
            {
                name = definition.Name;
                description = definition.Description;
                category = definition.Category;
            }
        }

        return new CodexTraitEntry
        {
            TraitTag = evt.TraitTag,
            Name = name,
            Description = description,
            Category = category,
            AcquisitionMethod = evt.AcquisitionMethod,
            DateAcquired = evt.OccurredAt
        };
    }

}

