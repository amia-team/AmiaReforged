using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage.Commands;

/// <summary>
/// Command to withdraw an item from personal storage.
/// </summary>
public record WithdrawItemCommand(
    long StoredItemId,
    Guid CharacterId) : ICommand;
