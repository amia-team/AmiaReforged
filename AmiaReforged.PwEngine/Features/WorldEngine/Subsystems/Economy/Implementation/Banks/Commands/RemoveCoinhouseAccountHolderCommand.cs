using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;

/// <summary>
/// Command to remove an account holder from a coinhouse account.
/// </summary>
/// <param name="Requestor">The persona requesting the removal (must have ManageHolders permission).</param>
/// <param name="AccountId">The ID of the coinhouse account.</param>
/// <param name="CoinhouseTag">The tag of the coinhouse where the account is held.</param>
/// <param name="HolderIdToRemove">The ID of the holder to remove from the account.</param>
public sealed record RemoveCoinhouseAccountHolderCommand(
    PersonaId Requestor,
    Guid AccountId,
    CoinhouseTag CoinhouseTag,
    Guid HolderIdToRemove) : ICommand;
