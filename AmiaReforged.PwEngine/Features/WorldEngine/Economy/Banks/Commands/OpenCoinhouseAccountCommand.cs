using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Commands;

/// <summary>
/// Opens a new coinhouse account for a persona, optionally on behalf of an organization.
/// </summary>
public sealed record OpenCoinhouseAccountCommand(
    PersonaId Requestor,
    PersonaId AccountPersona,
    CoinhouseTag Coinhouse,
    string? AccountDisplayName = null,
    IReadOnlyList<CoinhouseAccountHolderDto>? AdditionalHolders = null) : ICommand;
