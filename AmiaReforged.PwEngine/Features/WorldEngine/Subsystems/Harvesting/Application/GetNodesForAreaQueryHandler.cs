using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Application;

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

