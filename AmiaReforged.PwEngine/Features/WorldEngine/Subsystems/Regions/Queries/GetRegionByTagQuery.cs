using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Queries;

/// <summary>
/// Query to get a region definition by its tag.
/// </summary>
public sealed record GetRegionByTagQuery(RegionTag Tag) : IQuery<RegionDefinition?>;
