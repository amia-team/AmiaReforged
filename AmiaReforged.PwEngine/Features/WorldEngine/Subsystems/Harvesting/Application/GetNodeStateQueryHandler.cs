using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Application;

[ServiceBinding(typeof(IQueryHandler<GetNodeStateQuery, NodeStateDto?>))]
public class GetNodeStateQueryHandler(IResourceNodeInstanceRepository nodeRepository)
    : IQueryHandler<GetNodeStateQuery, NodeStateDto?>
{
    public Task<NodeStateDto?> HandleAsync(GetNodeStateQuery query, CancellationToken cancellationToken = default)
    {
        ResourceNodeInstance? node = nodeRepository.GetInstances().FirstOrDefault(n => n.Id == query.NodeInstanceId);

        if (node == null)
        {
            return Task.FromResult<NodeStateDto?>(null);
        }

        NodeStateDto dto = new NodeStateDto(
            node.Id,
            node.Definition.Tag,
            node.Uses,
            node.Quality,
            node.Area);

        return Task.FromResult<NodeStateDto?>(dto);
    }
}

