using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Traits.Handlers;

/// <summary>
/// Handles TraitActiveStateChangedEvent to apply or remove trait effects.
/// Currently logs the event - trait effect application will be implemented in integration layer.
/// </summary>
[ServiceBinding(typeof(IEventHandler<TraitActiveStateChangedEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]
public class TraitActiveStateChangedEventHandler(
    ITraitRepository traitRepository)
    : IEventHandler<TraitActiveStateChangedEvent>, IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task HandleAsync(TraitActiveStateChangedEvent @event, CancellationToken cancellationToken = default)
    {
        // No NWN calls needed currently - trait effects applied in integration layer
        await Task.CompletedTask;

        Trait? trait = traitRepository.Get(@event.TraitTag.Value);
        string traitName = trait?.Name ?? @event.TraitTag.Value;
        string action = @event.IsActive ? "activated" : "deactivated";

        Log.Info($"Trait {action}: CharacterId={@event.CharacterId.Value}, Trait={traitName}");

        // Future enhancements:
        // - Update UI displays to show active/inactive status
        // - Trigger visual effects when traits are toggled
        // - Send notifications to player
        // - Log to character audit trail

        // Note: Actual trait effect application (stat bonuses, abilities, etc.)
        // is handled by the integration layer when character data is loaded/saved
    }
}

