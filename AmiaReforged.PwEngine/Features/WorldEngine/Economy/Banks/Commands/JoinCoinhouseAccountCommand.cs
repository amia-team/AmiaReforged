using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Access;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Commands;

public sealed record JoinCoinhouseAccountCommand(
    PersonaId Requestor,
    Guid AccountId,
    CoinhouseTag Coinhouse,
    BankShareType ShareType,
    Guid DocumentId,
    DateTime IssuedAtUtc,
    string IssuerName,
    HolderType HolderType,
    HolderRole Role,
    string HolderFirstName,
    string HolderLastName) : ICommand;
