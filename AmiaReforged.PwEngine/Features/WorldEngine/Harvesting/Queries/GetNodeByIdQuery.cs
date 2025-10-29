using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Queries;

/// <summary>
/// Query to get a specific resource node by ID
/// </summary>
public record GetNodeByIdQuery(Guid NodeInstanceId) : IQuery<ResourceNodeInstance?>;

