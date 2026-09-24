using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaGraph;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.AreaGraph.Queries;

/// <summary>
/// Returns the current area connectivity graph from the cache service.
/// Read-only: never forces a rebuild — forced refresh is handled separately.
/// </summary>
public record GetAreaGraphQuery : IQuery<AreaGraphData>
{
}
