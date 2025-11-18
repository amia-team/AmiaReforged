using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Treasuries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

[ServiceBinding(typeof(IReeveFundsService))]
[ServiceBinding(typeof(ReeveFundsService))]
public sealed class ReeveFundsService : IReeveFundsService
{
    private readonly IVaultRepository _vaultRepository;

    public ReeveFundsService(IVaultRepository vaultRepository)
    {
        _vaultRepository = vaultRepository ?? throw new ArgumentNullException(nameof(vaultRepository));
    }

    public async Task<int> GetHeldFundsAsync(PersonaId persona, string? areaResRef, CancellationToken ct = default)
    {
        Guid owner = PersonaId.ToGuid(persona);
        string area = areaResRef ?? string.Empty;

        return await _vaultRepository.GetBalanceAsync(owner, area, ct);
    }

    public async Task<int> ReleaseHeldFundsAsync(PersonaId persona, string? areaResRef, int requestedAmount,
        Func<int, Task<bool>> grantToPlayerAsync,
        CancellationToken ct = default)
    {
        Guid owner = PersonaId.ToGuid(persona);
        string area = areaResRef ?? string.Empty;

        // Determine amount to withdraw (0 = all)
        int amount = requestedAmount <= 0
            ? await GetHeldFundsAsync(persona, areaResRef, ct)
            : requestedAmount;

        if (amount <= 0)
        {
            return 0;
        }

        // Withdraw from vault
        int withdrawn = await _vaultRepository.WithdrawAsync(owner, area, amount, ct);

        if (withdrawn <= 0)
        {
            return 0;
        }

        // Grant to player
        bool granted = await grantToPlayerAsync(withdrawn);
        if (!granted)
        {
            // Rollback: re-deposit to vault
            await _vaultRepository.DepositAsync(owner, area, withdrawn, ct);
            return 0;
        }

        return withdrawn;
    }

    /// <summary>
    /// Deposits funds into a vault (for stall escrow, etc.).
    /// </summary>
    public async Task<CommandResult> DepositHeldFundsAsync(
        PersonaId persona,
        string? areaResRef,
        int amount,
        string reason,
        CancellationToken ct = default)
    {
        Guid owner = PersonaId.ToGuid(persona);
        string area = areaResRef ?? string.Empty;

        try
        {
            int deposited = await _vaultRepository.DepositAsync(owner, area, amount, ct);

            if (deposited > 0)
            {
                return CommandResult.Ok();
            }

            return CommandResult.Fail($"Failed to deposit {amount} to vault. No amount was deposited.");
        }
        catch (Exception ex)
        {
            return CommandResult.Fail($"Failed to deposit {amount} to vault: {ex.Message}");
        }
    }
}
