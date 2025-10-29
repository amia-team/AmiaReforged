using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions.Application;

[ServiceBinding(typeof(ICommandHandler<ClearAllRegionsCommand>))]
public class ClearAllRegionsCommandHandler(
    IRegionRepository repository,
    IEventBus eventBus) : ICommandHandler<ClearAllRegionsCommand>
{
    public async Task<CommandResult> HandleAsync(ClearAllRegionsCommand command, CancellationToken cancellationToken = default)
    {
        // Get count before clearing
        int regionCount = repository.All().Count;

        // Clear all regions
        repository.Clear();

        // Publish event
        await eventBus.PublishAsync(new AllRegionsClearedEvent(
            regionCount,
            DateTime.UtcNow), cancellationToken);

        return CommandResult.OkWith("clearedCount", regionCount);
    }
}

