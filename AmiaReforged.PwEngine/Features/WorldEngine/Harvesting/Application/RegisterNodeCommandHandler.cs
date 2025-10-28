using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Application;

[ServiceBinding(typeof(ICommandHandler<RegisterNodeCommand>))]
public class RegisterNodeCommandHandler(
    IResourceNodeInstanceRepository nodeRepository,
    IResourceNodeDefinitionRepository definitionRepository,
    IEventBus eventBus) : ICommandHandler<RegisterNodeCommand>
{
    public async Task<CommandResult> HandleAsync(RegisterNodeCommand command, CancellationToken cancellationToken = default)
    {
        // Find the definition
        ResourceNodeDefinition? definition = definitionRepository.Get(command.ResourceTag);
        if (definition == null)
        {
            return CommandResult.Fail($"Resource definition '{command.ResourceTag}' not found");
        }

        // Create the instance
        ResourceNodeInstance instance = new ResourceNodeInstance
        {
            Area = command.AreaResRef,
            Definition = definition,
            X = command.X,
            Y = command.Y,
            Z = command.Z,
            Rotation = command.Rotation,
            Quality = command.Quality,
            Uses = command.Uses
        };

        // Save to repository
        nodeRepository.AddNodeInstance(instance);
        nodeRepository.SaveChanges();

        // Publish event
        await eventBus.PublishAsync(new NodeRegisteredEvent(
            instance.Id,
            command.AreaResRef,
            command.ResourceTag,
            command.Quality,
            command.Uses,
            DateTime.UtcNow), cancellationToken);

        return CommandResult.OkWith("nodeInstanceId", instance.Id);
    }
}

