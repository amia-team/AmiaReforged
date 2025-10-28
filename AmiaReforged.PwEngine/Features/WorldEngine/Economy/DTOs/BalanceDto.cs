using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.DTOs;

/// <summary>
/// Data transfer object representing a persona's balance at a specific coinhouse.
/// Returned by balance queries.
/// </summary>
public sealed record BalanceDto
{
    public required PersonaId PersonaId { get; init; }
    public required CoinhouseTag Coinhouse { get; init; }
    public required int Balance { get; init; }
    public required DateTime? LastAccessedAt { get; init; }

    public static BalanceDto Create(
        PersonaId personaId,
        CoinhouseTag coinhouse,
        int balance,
        DateTime? lastAccessedAt = null)
    {
        return new BalanceDto
        {
            PersonaId = personaId,
            Coinhouse = coinhouse,
            Balance = balance,
            LastAccessedAt = lastAccessedAt
        };
    }
}

