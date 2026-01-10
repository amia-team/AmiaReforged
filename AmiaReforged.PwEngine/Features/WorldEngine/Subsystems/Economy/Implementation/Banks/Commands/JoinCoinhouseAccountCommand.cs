using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Access;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;

/// <summary>
/// Command to add a holder to a coinhouse account. When NewHolder is specified, the Requestor is adding
/// someone else (must have IssueShares permission). When NewHolder is null, the Requestor is joining themselves.
/// </summary>
public sealed record JoinCoinhouseAccountCommand(
    PersonaId Requestor,
    Guid AccountId,
    CoinhouseTag Coinhouse,
    BankShareType ShareType,
    HolderType HolderType,
    HolderRole Role,
    string HolderFirstName,
    string HolderLastName,
    PersonaId? NewHolder = null) : ICommand;
