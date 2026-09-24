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
/// <see cref="DynamicQuestService"/> only publishes to <see cref="IEventBus"/>; this
/// subscriber is the sole forwarder for the events that carry a Codex mutation:
/// <see cref="QuestClaimedEvent"/>, <see cref="QuestUnclaimedEvent"/>, and
/// <see cref="QuestExpiredEvent"/>. <see cref="QuestPostedEvent"/> and
/// <see cref="QuestSharedEvent"/> are intentionally not forwarded — the former carries
/// no Codex mutation and the latter only audits the claimant (the invitee receives a
/// separate <see cref="QuestClaimedEvent"/>).
/// </para>
/// </summary>
[ServiceBinding(typeof(IEventHandlerMarker))]
public sealed class DynamicQuestCodexEventForwarder
    : IEventHandler<QuestClaimedEvent>,
      IEventHandler<QuestUnclaimedEvent>,
      IEventHandler<QuestExpiredEvent>,
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
}
