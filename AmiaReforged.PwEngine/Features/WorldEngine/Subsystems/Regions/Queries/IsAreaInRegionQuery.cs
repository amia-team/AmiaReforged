using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Queries;

/// <summary>
/// Returns true when the area (by resref) is defined in any region.
/// Unregistered areas return <see langword="false"/> — they receive no chaos state,
/// only mutations (profile bonuses).
/// </summary>
public sealed record IsAreaInRegionQuery(string AreaResRef) : IQuery<bool>;
