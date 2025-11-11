using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Application;

[ServiceBinding(typeof(IQueryHandler<GetNodeByIdQuery, ResourceNodeInstance?>))]
public class GetNodeByIdQueryHandler(IResourceNodeInstanceRepository nodeRepository)
    : IQueryHandler<GetNodeByIdQuery, ResourceNodeInstance?>
{
    public Task<ResourceNodeInstance?> HandleAsync(GetNodeByIdQuery query, CancellationToken cancellationToken = default)
    {
        ResourceNodeInstance? node = nodeRepository.GetInstances().FirstOrDefault(n => n.Id == query.NodeInstanceId);
        return Task.FromResult(node);
    }
}

