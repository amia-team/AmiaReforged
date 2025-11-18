using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;
public interface IReeveFundsService
{
    Task<int> GetHeldFundsAsync(PersonaId persona, string? areaResRef, CancellationToken ct = default);
    Task<int> ReleaseHeldFundsAsync(PersonaId persona, string? areaResRef, int requestedAmount,
        System.Func<int, Task<bool>> grantToPlayerAsync,
        CancellationToken ct = default);
    Task<CommandResult> DepositHeldFundsAsync(
        PersonaId persona,
        string? areaResRef,
        int amount,
        string reason,
        CancellationToken ct = default);
}
