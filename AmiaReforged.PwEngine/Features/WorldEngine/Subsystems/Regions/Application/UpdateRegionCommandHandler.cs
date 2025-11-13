using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Application;

[ServiceBinding(typeof(ICommandHandler<Commands.UpdateRegionCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public class UpdateRegionCommandHandler(
    IRegionRepository repository,
    IEventBus eventBus) : ICommandHandler<Commands.UpdateRegionCommand>
{
    public async Task<CommandResult> HandleAsync(Commands.UpdateRegionCommand command, CancellationToken cancellationToken = default)
    {
        // Check if region exists
        if (!repository.Exists(command.Tag))
        {
            return CommandResult.Fail($"Region with tag '{command.Tag}' not found");
        }

        // Get existing definition
        RegionDefinition? existing = repository.All().FirstOrDefault(r => r.Tag.Value == command.Tag.Value);
        if (existing == null)
        {
            return CommandResult.Fail($"Region with tag '{command.Tag}' not found");
        }

        // Create updated definition (only update provided fields)
        RegionDefinition updated = new()
        {
            Tag = command.Tag,
            Name = command.Name ?? existing.Name,
            Areas = command.Areas ?? existing.Areas
        };

        // Validate
        if (string.IsNullOrWhiteSpace(updated.Name))
        {
            return CommandResult.Fail("Region name cannot be empty");
        }

        if (updated.Areas == null || updated.Areas.Count == 0)
        {
            return CommandResult.Fail("Region must have at least one area");
        }

        // Update repository
        repository.Update(updated);

        // Publish event
        await eventBus.PublishAsync(new RegionUpdatedEvent(
            command.Tag,
            updated.Name,
            DateTime.UtcNow), cancellationToken);

        return CommandResult.OkWith("tag", command.Tag.Value);
    }
}

