using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;

/// <summary>
/// Synchronizes dialogue-definition NPCs from successful command events.
///
/// Runs on the asynchronous production event bus (<see cref="Anvil.Services.AnvilEventBusService"/>)
/// after the <c>CommandDispatcher</c> has published a
/// <see cref="CommandExecutedEvent{TCommand}"/> for a successful dialogue-tree
/// create/update/delete command. It is orchestration only: it reads the identifiers already
/// carried by the command and delegates the actual NPC work to
/// <see cref="IDialogueNpcSynchronizer"/> (implemented by <see cref="DialogueNpcHook"/>), which
/// owns the tree/tag registry and performs the NWN main-thread transition. This handler never
/// touches NWN object APIs directly.
///
/// Because the dispatcher only publishes these events for successful results, a failed command
/// never reaches this subscriber. The defensive <see cref="Result.Success"/> check below never
/// turns a valid event into a failure; it is kept for defensive symmetry with the sibling
/// <see cref="ExecuteDialogueActionHandler"/> in this same subsystem. The dispatcher cannot
/// publish a non-success event, so the guarded path is unreachable in practice.
/// </summary>
[ServiceBinding(typeof(DialogueNpcSynchronizationHandler))]
public sealed class DialogueNpcSynchronizationHandler
    : IEventHandler<CommandExecutedEvent<CreateDialogueTreeCommand>>,
      IEventHandler<CommandExecutedEvent<UpdateDialogueTreeCommand>>,
      IEventHandler<CommandExecutedEvent<DeleteDialogueTreeCommand>>,
      IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    [Inject]
    internal IDialogueNpcSynchronizer? Synchronizer { get; init; }

    /// <summary>
    /// A newly created dialogue tree registers NPCs for its speaker tag. A null/empty/whitespace
    /// tag means the tree has no NPC, so no registration call is made. The tag is carried by the
    /// command; the database is not re-queried to recover it.
    /// </summary>
    public async Task HandleAsync(
        CommandExecutedEvent<CreateDialogueTreeCommand> @event,
        CancellationToken cancellationToken = default)
    {
        if (!@event.Result.Success)
            return;

        string? speakerTag = @event.Command.Tree.SpeakerTag;
        if (string.IsNullOrWhiteSpace(speakerTag))
        {
            Log.Debug(
                "DialogueNpcSynchronizationHandler: create tree '{TreeId}' has no speaker tag — skipping registration",
                @event.Command.Tree.DialogueTreeId);
            return;
        }

        int registered = await Synchronizer!.RegisterAsync(speakerTag, @event.Command.Tree.DialogueTreeId, cancellationToken)
            .ConfigureAwait(false);

        Log.Info(
            "DialogueNpcSynchronizationHandler: created tree '{TreeId}' (tag '{Tag}') registered {Count} NPC(s)",
            @event.Command.Tree.DialogueTreeId, speakerTag, registered);
    }

    /// <summary>
    /// An updated dialogue tree always re-syncs its speaker tag. The synchronizer owns the old-tag
    /// lookup through its registry, so the subscriber does not duplicate old-tag tracking. A null
    /// new tag is forwarded (to clear the tag), never discarded.
    /// </summary>
    public async Task HandleAsync(
        CommandExecutedEvent<UpdateDialogueTreeCommand> @event,
        CancellationToken cancellationToken = default)
    {
        if (!@event.Result.Success)
            return;

        (int unregistered, int registered) = await Synchronizer!.UpdateAsync(
            @event.Command.DialogueTreeId, @event.Command.Tree.SpeakerTag, cancellationToken)
            .ConfigureAwait(false);

        Log.Info(
            "DialogueNpcSynchronizationHandler: updated tree '{TreeId}' " +
            "(tag '{NewTag}') unregistered {Unregistered}, registered {Registered}",
            @event.Command.DialogueTreeId,
            @event.Command.Tree.SpeakerTag ?? "(none)",
            unregistered, registered);
    }

    /// <summary>
    /// A deleted dialogue tree unregisters its NPCs. Deletion happens before the event, so the
    /// synchronizer identifies the previously registered tag from the tree ID via its runtime
    /// registry. The deleted database row is never re-queried.
    /// </summary>
    public async Task HandleAsync(
        CommandExecutedEvent<DeleteDialogueTreeCommand> @event,
        CancellationToken cancellationToken = default)
    {
        if (!@event.Result.Success)
            return;

        int unregistered = await Synchronizer!.UnregisterAsync(@event.Command.DialogueTreeId, cancellationToken)
            .ConfigureAwait(false);

        Log.Info(
            "DialogueNpcSynchronizationHandler: deleted tree '{TreeId}' unregistered {Count} NPC(s)",
            @event.Command.DialogueTreeId, unregistered);
    }
}
