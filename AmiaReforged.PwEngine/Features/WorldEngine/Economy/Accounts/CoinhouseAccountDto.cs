using System;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;

/// <summary>
/// Lightweight representation of a coinhouse account for domain-facing operations.
/// </summary>
public sealed record CoinhouseAccountDto
{
    public required Guid Id { get; init; }
    public required int Debit { get; init; }
    public required int Credit { get; init; }
    public required long CoinHouseId { get; init; }
    public required DateTime OpenedAt { get; init; }
    public DateTime? LastAccessedAt { get; init; }
    public CoinhouseDto? Coinhouse { get; init; }

    public int Balance => Debit - Credit;
}
