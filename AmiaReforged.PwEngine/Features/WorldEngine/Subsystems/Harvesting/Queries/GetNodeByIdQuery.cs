using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Queries;

/// <summary>
/// Query to get a specific resource node by ID
/// </summary>
public record GetNodeByIdQuery(Guid NodeInstanceId) : IQuery<ResourceNodeInstance?>;

