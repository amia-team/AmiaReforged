using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions.Application;

[ServiceBinding(typeof(ICommandHandler<RemoveRegionCommand>))]
public class RemoveRegionCommandHandler(
    IRegionRepository repository,
    IEventBus eventBus) : ICommandHandler<RemoveRegionCommand>
{
    public async Task<CommandResult> HandleAsync(RemoveRegionCommand command, CancellationToken cancellationToken = default)
    {
        // Check if region exists
        if (!repository.Exists(command.Tag))
        {
            return CommandResult.Fail($"Region with tag '{command.Tag}' not found");
        }

        // Get the definition to remove
        RegionDefinition? definition = repository.All().FirstOrDefault(r => r.Tag.Value == command.Tag.Value);
        if (definition == null)
        {
            return CommandResult.Fail($"Region with tag '{command.Tag}' not found");
        }

        // Remove from repository
        // Note: The current IRegionRepository doesn't have a Remove method, so we'll need to update it
        // For now, we'll work with what we have and clear/re-add without this region
        List<RegionDefinition> allRegions = repository.All();
        allRegions.RemoveAll(r => r.Tag.Value == command.Tag.Value);

        repository.Clear();
        foreach (RegionDefinition region in allRegions)
        {
            repository.Add(region);
        }

        // Publish event
        await eventBus.PublishAsync(new RegionRemovedEvent(
            command.Tag,
            DateTime.UtcNow), cancellationToken);

        return CommandResult.OkWith("tag", command.Tag.Value);
    }
}

