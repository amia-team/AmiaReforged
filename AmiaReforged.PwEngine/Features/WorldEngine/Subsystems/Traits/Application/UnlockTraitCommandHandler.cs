using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Application;

[ServiceBinding(typeof(ICommandHandler<UnlockTraitCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public class UnlockTraitCommandHandler(
    ITraitRepository traitRepository,
    IEventBus eventBus) : ICommandHandler<UnlockTraitCommand>
{
    public async Task<CommandResult> HandleAsync(UnlockTraitCommand command, CancellationToken cancellationToken = default)
    {
        // Validate trait exists
        Trait? trait = traitRepository.Get(command.TraitTag.Value);
        if (trait == null)
        {
            return CommandResult.Fail($"Trait '{command.TraitTag.Value}' not found");
        }

        // Note: This command just publishes the event. The actual unlock tracking
        // is handled by the system that maintains the unlocked traits dictionary.
        // This is intentionally simple to avoid coupling to unlock storage.

        // Publish event
        await eventBus.PublishAsync(new TraitUnlockedEvent(
            command.CharacterId,
            command.TraitTag,
            DateTime.UtcNow), cancellationToken);

        return CommandResult.Ok();
    }
}

