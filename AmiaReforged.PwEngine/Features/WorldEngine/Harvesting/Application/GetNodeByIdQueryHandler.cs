using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Application;

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

