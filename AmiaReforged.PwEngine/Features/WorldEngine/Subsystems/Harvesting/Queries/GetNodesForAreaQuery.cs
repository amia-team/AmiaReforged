using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Queries;

/// <summary>
/// Query to get all resource nodes in a specific area
/// </summary>
public record GetNodesForAreaQuery(string AreaResRef) : IQuery<List<ResourceNodeInstance>>;

