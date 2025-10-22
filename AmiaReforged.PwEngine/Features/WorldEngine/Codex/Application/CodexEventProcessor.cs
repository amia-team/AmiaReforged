using System.Threading.Channels;
using AmiaReforged.PwEngine.Features.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Codex.Application;

/// <summary>
/// Processes codex events from a channel and applies them to PlayerCodex aggregates.
/// Ensures sequential processing per character to maintain consistency.
/// </summary>
public class CodexEventProcessor
{
    private readonly IPlayerCodexRepository _repository;
    private readonly Channel<CodexDomainEvent> _eventChannel;
    private readonly CancellationTokenSource _cts;
    private Task? _processingTask;

    public CodexEventProcessor(IPlayerCodexRepository repository, Channel<CodexDomainEvent>? channel = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventChannel = channel ?? Channel.CreateUnbounded<CodexDomainEvent>();
        _cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Starts the event processor
    /// </summary>
    public void Start()
    {
        if (_processingTask != null)
            throw new InvalidOperationException("Event processor is already running");

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

        await foreach (var domainEvent in _eventChannel.Reader.ReadAllAsync(cancellationToken))
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
        var codex = await _repository.LoadAsync(domainEvent.CharacterId, cancellationToken)
                    ?? new PlayerCodex(domainEvent.CharacterId, domainEvent.OccurredAt);

        // Apply event to aggregate
        ApplyEvent(codex, domainEvent);

        // Save updated codex
        await _repository.SaveAsync(codex, cancellationToken);
    }

    /// <summary>
    /// Applies a domain event to the PlayerCodex aggregate
    /// </summary>
    private void ApplyEvent(PlayerCodex codex, CodexDomainEvent domainEvent)
    {
        // TODO: Implement event application logic
        // Use pattern matching on event types to call appropriate aggregate methods

        switch (domainEvent)
        {
            case QuestStartedEvent qse:
                // TODO: Create CodexQuestEntry from event and call codex.RecordQuestStarted()
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
                // TODO: Create CodexLoreEntry from event and call codex.RecordLoreDiscovered()
                break;

            case ReputationChangedEvent rce:
                // TODO: FactionName should be added to ReputationChangedEvent
                // For now, use FactionId as name
                codex.RecordReputationChange(rce.FactionId, rce.FactionId.Value, rce.Delta, rce.Reason, rce.OccurredAt);
                break;

            case NoteAddedEvent nae:
                // TODO: Create CodexNoteEntry from event and call codex.AddNote()
                break;

            case NoteEditedEvent nee:
                codex.EditNote(nee.NoteId, nee.NewContent, nee.OccurredAt);
                break;

            case NoteDeletedEvent nde:
                codex.DeleteNote(nde.NoteId, nde.OccurredAt);
                break;

            default:
                throw new NotSupportedException($"Event type {domainEvent.GetType().Name} is not supported");
        }
    }
}

/// <summary>
/// Repository interface for PlayerCodex persistence
/// </summary>
public interface IPlayerCodexRepository
{
    Task<PlayerCodex?> LoadAsync(CharacterId characterId, CancellationToken cancellationToken = default);
    Task SaveAsync(PlayerCodex codex, CancellationToken cancellationToken = default);
}
