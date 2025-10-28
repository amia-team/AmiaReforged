using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Queries;

/// <summary>
/// Query to get the current state of a resource node
/// </summary>
public record GetNodeStateQuery(Guid NodeInstanceId) : IQuery<NodeStateDto?>;

public record NodeStateDto(
    Guid NodeInstanceId,
    string ResourceTag,
    int RemainingUses,
    IPQuality Quality,
    string AreaResRef);


