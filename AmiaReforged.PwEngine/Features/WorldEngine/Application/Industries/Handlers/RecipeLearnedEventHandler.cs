using AmiaReforged.PwEngine.Features.WorldEngine.Industries.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Handlers;

/// <summary>
/// Handles RecipeLearnedEvent to track character progression.
/// Currently logs the event - can be extended for codex updates, achievements, etc.
/// </summary>
[ServiceBinding(typeof(IEventHandler<RecipeLearnedEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]
public class RecipeLearnedEventHandler
    : IEventHandler<RecipeLearnedEvent>, IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task HandleAsync(RecipeLearnedEvent @event, CancellationToken cancellationToken = default)
    {
        // No NWN calls needed currently
        await Task.CompletedTask;

        Log.Info($"Recipe learned: CharacterId={@event.LearnerId.Value}, Industry={@event.IndustryTag.Value}, Knowledge={@event.KnowledgeTag}, Cost={@event.PointCost}");

        // Future enhancements:
        // - Update codex with new recipe knowledge
        // - Grant achievements for milestone recipes
        // - Update UI to show newly available crafting options
        // - Send notification to player
        // - Track learning statistics for analytics
    }
}

