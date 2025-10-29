using AmiaReforged.PwEngine.Features.WorldEngine.Industries.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Handlers;

/// <summary>
/// Handles ProficiencyGainedEvent to track character progression.
/// Currently logs the event - can be extended for notifications, UI updates, etc.
/// </summary>
[ServiceBinding(typeof(IEventHandler<ProficiencyGainedEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]
public class ProficiencyGainedEventHandler
    : IEventHandler<ProficiencyGainedEvent>, IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task HandleAsync(ProficiencyGainedEvent @event, CancellationToken cancellationToken = default)
    {
        // No NWN calls needed currently
        await Task.CompletedTask;

        Log.Info($"Proficiency gained: CharacterId={@event.MemberId.Value}, Industry={@event.IndustryTag.Value}, NewLevel={@event.NewLevel}, PreviousLevel={@event.PreviousLevel}");

        // Future enhancements:
        // - Send congratulatory message to player
        // - Update character sheet UI
        // - Check for achievement unlocks
        // - Grant milestone rewards
        // - Update visible rank badges/titles
    }
}

