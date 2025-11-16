using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;
/// <summary>
/// Query to get a character's vault balance in a specific area.
/// Returns the current balance, or 0 if no vault exists.
/// </summary>
public sealed record GetVaultBalanceQuery(
    CharacterId Owner,
    string AreaResRef) : IQuery<int>;
