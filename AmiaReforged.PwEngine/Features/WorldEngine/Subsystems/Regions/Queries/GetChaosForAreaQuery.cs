using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Queries;

/// <summary>
/// Resolves the chaos state for an area. Returns the area-level chaos override when present,
/// otherwise the region's default chaos, otherwise <see cref="ChaosState.Default"/>.
/// </summary>
public sealed record GetChaosForAreaQuery(string AreaResRef) : IQuery<ChaosState>;
