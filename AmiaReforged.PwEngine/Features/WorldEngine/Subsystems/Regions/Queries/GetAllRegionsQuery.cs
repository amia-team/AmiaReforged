using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Queries;

/// <summary>
/// Query to get all region definitions.
/// </summary>
public sealed record GetAllRegionsQuery : IQuery<List<RegionDefinition>>;

