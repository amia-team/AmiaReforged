using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;

/// <summary>
/// Command to update an account holder's role on a coinhouse account.
/// Note: Transferring ownership (promoting to Owner) is not permitted.
/// </summary>
/// <param name="Requestor">The persona requesting the role change (must have ManageHolders permission).</param>
/// <param name="AccountId">The ID of the coinhouse account.</param>
/// <param name="CoinhouseTag">The tag of the coinhouse where the account is held.</param>
/// <param name="HolderIdToUpdate">The ID of the holder whose role will be changed.</param>
/// <param name="NewRole">The new role to assign to the holder.</param>
public sealed record UpdateCoinhouseAccountHolderRoleCommand(
    PersonaId Requestor,
    Guid AccountId,
    CoinhouseTag CoinhouseTag,
    Guid HolderIdToUpdate,
    HolderRole NewRole) : ICommand;
