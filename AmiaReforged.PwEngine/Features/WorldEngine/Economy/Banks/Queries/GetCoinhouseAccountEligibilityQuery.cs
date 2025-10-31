using System;
using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Queries;

/// <summary>
/// Determines whether a persona can open a coinhouse account in the specified coinhouse and
/// which organizations they can represent when doing so.
/// </summary>
public sealed record GetCoinhouseAccountEligibilityQuery(PersonaId Persona, CoinhouseTag Coinhouse)
    : IQuery<CoinhouseAccountEligibilityResult>;

/// <summary>
/// Result payload describing account creation options for a persona at a coinhouse.
/// </summary>
public sealed record CoinhouseAccountEligibilityResult
{
    /// <summary>
    /// Indicates whether the coinhouse exists and is eligible for new accounts.
    /// </summary>
    public required bool CoinhouseExists { get; init; }

    /// <summary>
    /// Provides an error message when the coinhouse could not be resolved.
    /// </summary>
    public string? CoinhouseError { get; init; }

    /// <summary>
    /// Indicates whether the requesting persona may open a personal account.
    /// </summary>
    public required bool CanOpenPersonalAccount { get; init; }

    /// <summary>
    /// Details why the persona cannot open a personal account, when applicable.
    /// </summary>
    public string? PersonalAccountBlockedReason { get; init; }

    /// <summary>
    /// Enumerates organization options the persona may open accounts for.
    /// </summary>
    public IReadOnlyList<OrganizationAccountEligibility> Organizations { get; init; }
        = Array.Empty<OrganizationAccountEligibility>();

    public static CoinhouseAccountEligibilityResult CoinhouseUnavailable(string message) => new()
    {
        CoinhouseExists = false,
        CoinhouseError = message,
        CanOpenPersonalAccount = false,
        PersonalAccountBlockedReason = message,
        Organizations = Array.Empty<OrganizationAccountEligibility>()
    };
}

/// <summary>
/// Describes whether a specific organization can open an account and why or why not.
/// </summary>
public sealed record OrganizationAccountEligibility
{
    public required OrganizationId OrganizationId { get; init; }
    public required string OrganizationName { get; init; }
    public required bool CanOpen { get; init; }
    public bool AlreadyHasAccount { get; init; }
    public string? BlockedReason { get; init; }
}
