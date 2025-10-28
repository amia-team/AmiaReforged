using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Application;

[ServiceBinding(typeof(ICommandHandler<DestroyNodeCommand>))]
public class DestroyNodeCommandHandler(
    IResourceNodeInstanceRepository nodeRepository,
    IEventBus eventBus) : ICommandHandler<DestroyNodeCommand>
{
    public async Task<CommandResult> HandleAsync(DestroyNodeCommand command, CancellationToken cancellationToken = default)
    {
        ResourceNodeInstance? node = nodeRepository.GetInstances().FirstOrDefault(n => n.Id == command.NodeInstanceId);
        if (node == null)
        {
            return CommandResult.Fail("Node not found");
        }

        // Note: We publish NodeDepletedEvent even though it's being manually destroyed
        // The event name might be reconsidered in the future (e.g., NodeRemovedEvent)
        await eventBus.PublishAsync(new NodeDepletedEvent(
            node.Id,
            node.Area,
            node.Definition.Tag,
            Guid.Empty, // No harvester for manual destruction
            DateTime.UtcNow), cancellationToken);

        nodeRepository.Delete(node);
        nodeRepository.SaveChanges();

        return CommandResult.Ok();
    }
}

