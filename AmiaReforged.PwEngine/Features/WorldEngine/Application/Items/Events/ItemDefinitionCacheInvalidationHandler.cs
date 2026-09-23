using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Events;

/// <summary>
/// Invalidates the item blueprint expander's concrete-item cache whenever an item
/// definition is successfully created, replaced, or deleted.
///
/// <para>
/// This owns the invalidation that the HTTP controller used to perform directly, so that
/// every command caller (controllers, import jobs, future CLI/console callers) triggers the
/// same single path and no caller can forget it.
/// </para>
/// <para>
/// It reacts to <see cref="CommandExecutedEvent{TCommand}"/>, which the command dispatcher
/// publishes automatically and only for <b>successful</b> commands — rejected commands never
/// publish the event and therefore never invalidate.
/// </para>
/// <para>
/// <b>Read-after-write contract (asynchronous bus).</b> The production event bus
/// (<see cref="Anvil.Services"/> / <c>AnvilEventBusService</c>) queues events and runs
/// subscribers on a background thread, so invalidation is <b>eventually consistent</b>: a read
/// issued immediately after a successful command may still observe the previously expanded
/// concrete items until the background processor runs this handler. Correctness does not
/// depend on ordering, because the command handler commits the new/deleted blueprint to the
/// repository <i>synchronously</i> before the event is published; when the subscriber finally
/// runs, <see cref="ItemBlueprintExpander.Invalidate"/> re-expands from the already-fresh
/// repository state. Callers that must observe the change synchronously should await an
/// explicit invalidation rather than rely on the event.
/// </para>
/// </summary>
[ServiceBinding(typeof(IEventHandlerMarker))]
public sealed class ItemDefinitionCacheInvalidationHandler
    : IEventHandler<CommandExecutedEvent<UpsertItemDefinitionCommand>>,
      IEventHandler<CommandExecutedEvent<DeleteItemDefinitionCommand>>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ItemBlueprintExpander _expander;

    public ItemDefinitionCacheInvalidationHandler(ItemBlueprintExpander expander)
    {
        _expander = expander;
    }

    public Task HandleAsync(
        CommandExecutedEvent<UpsertItemDefinitionCommand> @event,
        CancellationToken cancellationToken = default)
    {
        string tag = @event.Command.Blueprint.ItemTag;
        Log.Debug("Item definition upserted at {At}; invalidating expander cache for '{Tag}'.",
            @event.OccurredAt, tag);
        _expander.Invalidate();
        return Task.CompletedTask;
    }

    public Task HandleAsync(
        CommandExecutedEvent<DeleteItemDefinitionCommand> @event,
        CancellationToken cancellationToken = default)
    {
        string tag = @event.Command.Tag;
        Log.Debug("Item definition deleted at {At}; invalidating expander cache for '{Tag}'.",
            @event.OccurredAt, tag);
        _expander.Invalidate();
        return Task.CompletedTask;
    }
}
