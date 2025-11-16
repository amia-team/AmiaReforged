using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

[ServiceBinding(typeof(IReeveFundsService))]
[ServiceBinding(typeof(ReeveFundsService))]
public sealed class ReeveFundsService : IReeveFundsService
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public ReeveFundsService(ICommandDispatcher commands, IQueryDispatcher queries)
    {
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<int> GetHeldFundsAsync(PersonaId persona, string? areaResRef, CancellationToken ct = default)
    {
        CharacterId owner = CharacterId.From(PersonaId.ToGuid(persona));
        string area = areaResRef ?? string.Empty;

        GetVaultBalanceQuery query = new(owner, area);
        return await _queries.DispatchAsync<GetVaultBalanceQuery, int>(query, ct);
    }

    public async Task<int> ReleaseHeldFundsAsync(PersonaId persona, string? areaResRef, int requestedAmount,
        Func<int, Task<bool>> grantToPlayerAsync,
        CancellationToken ct = default)
    {
        CharacterId owner = CharacterId.From(PersonaId.ToGuid(persona));
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
        WithdrawFromVaultCommand withdrawCmd = WithdrawFromVaultCommand.Create(
            owner,
            area,
            amount,
            "Market reeve funds release");

        CommandResult withdrawResult = await _commands.DispatchAsync(withdrawCmd, ct);

        if (!withdrawResult.Success || withdrawResult.Data == null)
        {
            return 0;
        }

        int withdrawn = (int)withdrawResult.Data["withdrawnAmount"];

        // Grant to player
        bool granted = await grantToPlayerAsync(withdrawn);
        if (!granted)
        {
            // Rollback: re-deposit to vault
            DepositToVaultCommand depositCmd = DepositToVaultCommand.Create(
                owner,
                area,
                withdrawn,
                "Market reeve funds release rollback");

            await _commands.DispatchAsync(depositCmd, ct);
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
        CharacterId owner = CharacterId.From(PersonaId.ToGuid(persona));
        string area = areaResRef ?? string.Empty;

        DepositToVaultCommand cmd = DepositToVaultCommand.Create(owner, area, amount, reason);
        return await _commands.DispatchAsync(cmd, ct);
    }
}
