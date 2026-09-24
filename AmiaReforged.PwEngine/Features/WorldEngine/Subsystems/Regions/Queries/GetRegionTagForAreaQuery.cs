using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Queries;

/// <summary>
/// Returns the region tag that defines the given area (by resref), or <see langword="null"/>
/// when the area is not defined in any region. Matching is case-insensitive.
/// </summary>
public sealed record GetRegionTagForAreaQuery(string AreaResRef) : IQuery<string?>;
