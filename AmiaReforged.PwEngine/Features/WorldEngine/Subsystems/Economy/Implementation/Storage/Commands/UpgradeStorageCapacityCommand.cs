using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Commands;

/// <summary>
/// Command to upgrade storage capacity by 10 slots.
/// Cost: 50k first upgrade, then +100k per tier.
/// Max capacity: 100 slots.
/// </summary>
public record UpgradeStorageCapacityCommand(
    CoinhouseTag CoinhouseTag,
    Guid CharacterId) : ICommand;
