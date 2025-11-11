using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;
/// <summary>
/// Query to get a persona's balance at a specific coinhouse.
/// Returns the current balance (debit - credit), or null if no account exists.
/// </summary>
public sealed record GetBalanceQuery(
    PersonaId PersonaId,
    CoinhouseTag Coinhouse) : IQuery<int?>;
