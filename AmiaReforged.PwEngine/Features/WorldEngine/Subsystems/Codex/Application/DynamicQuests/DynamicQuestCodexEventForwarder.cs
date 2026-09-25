using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.DynamicQuests;

/// <summary>
/// The single application path that routes dynamic-quest domain events published on
/// the shared bus into the existing <see cref="CodexEventProcessor"/> so the resulting
/// <see cref="AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities.PlayerCodex"/>
/// aggregate is mutated exactly once.
///
/// <para>
/// <see cref="DynamicQuestService"/> and <see cref="QuestObjectiveResolutionService"/>
/// only publish to <see cref="IEventBus"/>; this subscriber is the sole forwarder for
/// the events that carry a Codex mutation:
/// <see cref="QuestClaimedEvent"/>, <see cref="QuestUnclaimedEvent"/>,
/// <see cref="QuestExpiredEvent"/>, <see cref="QuestStageAdvancedEvent"/>, and
/// <see cref="StageRewardsGrantedEvent"/>.
/// <see cref="QuestPostedEvent"/> and <see cref="QuestSharedEvent"/> are intentionally not
/// forwarded — the former carries no Codex mutation and the latter only audits the
/// claimant (the invitee receives a separate <see cref="QuestClaimedEvent"/>).
/// Objective-state events (<see cref="ObjectiveProgressedEvent"/>,
/// <see cref="ObjectiveCompletedEvent"/>, <see cref="ObjectiveFailedEvent"/>,
/// <see cref="QuestObjectiveGroupCompletedEvent"/>) are published on the bus for
/// observability but are intentionally not forwarded: objective state lives on the
/// in-memory <see cref="QuestSession"/> and the codex does not persist per-objective
/// progress. Forwarding them would trigger pointless aggregate load/save work.
/// </para>
/// </summary>
[ServiceBinding(typeof(IEventHandlerMarker))]
public sealed class DynamicQuestCodexEventForwarder
    : IEventHandler<QuestClaimedEvent>,
      IEventHandler<QuestUnclaimedEvent>,
      IEventHandler<QuestExpiredEvent>,
      IEventHandler<QuestStageAdvancedEvent>,
      IEventHandler<StageRewardsGrantedEvent>,
      IEventHandlerMarker
{
    private readonly CodexEventProcessor _processor;

    public DynamicQuestCodexEventForwarder(CodexEventProcessor processor)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
    }

    public Task HandleAsync(QuestClaimedEvent @event, CancellationToken cancellationToken = default)
    {
        return _processor.EnqueueEventAsync(@event, cancellationToken);
    }

    public Task HandleAsync(QuestUnclaimedEvent @event, CancellationToken cancellationToken = default)
    {
        return _processor.EnqueueEventAsync(@event, cancellationToken);
    }

    public Task HandleAsync(QuestExpiredEvent @event, CancellationToken cancellationToken = default)
    {
        return _processor.EnqueueEventAsync(@event, cancellationToken);
    }

    public Task HandleAsync(QuestStageAdvancedEvent @event, CancellationToken cancellationToken = default)
    {
        return _processor.EnqueueEventAsync(@event, cancellationToken);
    }

    public Task HandleAsync(StageRewardsGrantedEvent @event, CancellationToken cancellationToken = default)
    {
        return _processor.EnqueueEventAsync(@event, cancellationToken);
    }
}
