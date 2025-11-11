using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.DTOs;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;
/// <summary>
/// Query to get all coinhouse balances for a persona.
/// Returns a collection of balances across all coinhouses where the persona has accounts.
/// </summary>
public sealed record GetCoinhouseBalancesQuery(
    PersonaId PersonaId) : IQuery<IReadOnlyList<BalanceDto>>;
