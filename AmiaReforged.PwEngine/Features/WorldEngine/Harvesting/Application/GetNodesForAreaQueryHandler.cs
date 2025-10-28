using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Application;

[ServiceBinding(typeof(IQueryHandler<GetNodesForAreaQuery, List<ResourceNodeInstance>>))]
public class GetNodesForAreaQueryHandler(IResourceNodeInstanceRepository nodeRepository)
    : IQueryHandler<GetNodesForAreaQuery, List<ResourceNodeInstance>>
{
    public Task<List<ResourceNodeInstance>> HandleAsync(GetNodesForAreaQuery query, CancellationToken cancellationToken = default)
    {
        List<ResourceNodeInstance> nodes = nodeRepository.GetInstancesByArea(query.AreaResRef);
        return Task.FromResult(nodes);
    }
}

