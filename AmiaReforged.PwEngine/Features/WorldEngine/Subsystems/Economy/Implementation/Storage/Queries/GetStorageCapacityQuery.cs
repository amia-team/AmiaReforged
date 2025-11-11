using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Queries;

/// <summary>
/// Query to get storage capacity information for a character at a specific bank.
/// </summary>
public record GetStorageCapacityQuery(
    CoinhouseTag CoinhouseTag,
    Guid CharacterId) : IQuery<GetStorageCapacityResult>;

/// <summary>
/// Result containing storage capacity information.
/// </summary>
public record GetStorageCapacityResult(
    int TotalCapacity,
    int UsedSlots,
    int AvailableSlots,
    bool CanUpgrade,
    int? NextUpgradeCost);
