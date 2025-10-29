using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions.Queries;

/// <summary>
/// Query to get all settlements for a region.
/// </summary>
public sealed record GetSettlementsForRegionQuery(RegionTag RegionTag) : IQuery<IReadOnlyCollection<SettlementId>>;

