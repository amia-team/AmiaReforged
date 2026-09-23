using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Events;

/// <summary>
/// Refreshes the recipe-expansion cache whenever a recipe template is created, updated, or deleted.
/// <para>
/// This replaces the previous direct <c>RecipeTemplateExpander.Invalidate()</c> calls that the
/// <c>RecipeTemplateController</c> made as a controller side effect. Invalidation is now an
/// asynchronous reaction to the <see cref="CommandExecutedEvent{TCommand}"/> domain event that the
/// command dispatcher publishes for every <em>successful</em> template mutation.
/// <para>
/// Freshness contract:
/// <list type="bullet">
/// <item>Invalidation fires only after the command commits successfully — a failed mutation never
/// invalidates, so a stale cache is never rebuilt from bad data.</item>
/// <item>It is decoupled from the HTTP layer: any caller that drives the mutations through the
/// command dispatcher (admin panel, bulk import, tests) gets the same behaviour.</item>
/// <item>Processing is asynchronous and runs on a background thread, so the mutating request does
/// not block on a full re-expansion. The next query triggers a fresh expansion on demand.</item>
/// </list>
/// </para>
/// </summary>
[ServiceBinding(typeof(IEventHandlerMarker))]
public sealed class RecipeTemplateCacheInvalidationHandler
    : IEventHandler<CommandExecutedEvent<CreateRecipeTemplateCommand>>,
      IEventHandler<CommandExecutedEvent<UpdateRecipeTemplateCommand>>,
      IEventHandler<CommandExecutedEvent<DeleteRecipeTemplateCommand>>,
      IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly RecipeTemplateExpander _expander;

    public RecipeTemplateCacheInvalidationHandler(RecipeTemplateExpander expander)
    {
        _expander = expander;
    }

    public Task HandleAsync(
        CommandExecutedEvent<CreateRecipeTemplateCommand> @event,
        CancellationToken cancellationToken = default)
    {
        return Invalidate(@event.Command.Template.Tag);
    }

    public Task HandleAsync(
        CommandExecutedEvent<UpdateRecipeTemplateCommand> @event,
        CancellationToken cancellationToken = default)
    {
        return Invalidate(@event.Command.Tag);
    }

    public Task HandleAsync(
        CommandExecutedEvent<DeleteRecipeTemplateCommand> @event,
        CancellationToken cancellationToken = default)
    {
        return Invalidate(@event.Command.Tag);
    }

    private Task Invalidate(string tag)
    {
        Log.Debug("Recipe template '{Tag}' changed — invalidating expansion cache.", tag);
        _expander.Invalidate();
        return Task.CompletedTask;
    }
}
