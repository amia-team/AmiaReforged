using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Queries;

/// <summary>
/// Query to get a region by settlement ID.
/// </summary>
public sealed record GetRegionBySettlementQuery(SettlementId SettlementId) : IQuery<RegionDefinition?>;

