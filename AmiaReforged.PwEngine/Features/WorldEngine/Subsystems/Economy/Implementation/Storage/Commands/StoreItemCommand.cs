using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Commands;

/// <summary>
/// Command to store an item in personal storage at a bank.
/// </summary>
public record StoreItemCommand(
    CoinhouseTag CoinhouseTag,
    Guid CharacterId,
    string ItemName,
    string ItemDescription,
    byte[] ItemData) : ICommand;
