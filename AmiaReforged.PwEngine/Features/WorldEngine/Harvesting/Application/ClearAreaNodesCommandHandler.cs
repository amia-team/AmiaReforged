using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Application;

[ServiceBinding(typeof(ICommandHandler<ClearAreaNodesCommand>))]
public class ClearAreaNodesCommandHandler(
    IResourceNodeInstanceRepository nodeRepository,
    IEventBus eventBus) : ICommandHandler<ClearAreaNodesCommand>
{
    public async Task<CommandResult> HandleAsync(ClearAreaNodesCommand command, CancellationToken cancellationToken = default)
    {
        List<ResourceNodeInstance> nodes = nodeRepository.GetInstancesByArea(command.AreaResRef);
        int count = nodes.Count;

        foreach (ResourceNodeInstance node in nodes)
        {
            nodeRepository.Delete(node);
        }

        nodeRepository.SaveChanges();

        await eventBus.PublishAsync(new NodesClearedEvent(
            command.AreaResRef,
            count,
            DateTime.UtcNow), cancellationToken);

        return CommandResult.OkWith("nodesCleared", count);
    }
}


