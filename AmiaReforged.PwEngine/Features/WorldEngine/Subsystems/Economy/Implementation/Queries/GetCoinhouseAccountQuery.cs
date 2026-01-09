using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;

/// <summary>
/// Retrieves the coinhouse account for a persona at a specific coinhouse.
/// Returns null if no account exists or the coinhouse cannot be resolved.
/// </summary>
public sealed record GetCoinhouseAccountQuery(PersonaId Persona, CoinhouseTag Coinhouse)
    : IQuery<CoinhouseAccountQueryResult?>;

/// <summary>
/// Lightweight projection of an account, including debit and credit tallies.
/// </summary>
public sealed record CoinhouseAccountQueryResult
{
    public required bool AccountExists { get; init; }
    public required Guid AccountId { get; init; }
    public required CoinhouseAccountSummary? Account { get; init; }
    public IReadOnlyList<CoinhouseAccountHolderDto> Holders { get; init; } = Array.Empty<CoinhouseAccountHolderDto>();
}

/// <summary>
/// Snapshot of account financials for UI consumption.
/// </summary>
public sealed record CoinhouseAccountSummary
{
    public required long CoinhouseId { get; init; }
    public required CoinhouseTag CoinhouseTag { get; init; }
    public required int Debit { get; init; }
    public required int Credit { get; init; }
    public required DateTime OpenedAt { get; init; }
    public DateTime? LastAccessedAt { get; init; }
}
