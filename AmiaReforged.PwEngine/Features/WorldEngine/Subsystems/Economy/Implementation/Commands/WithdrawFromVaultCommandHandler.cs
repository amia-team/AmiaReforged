using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Treasuries;
using Anvil.Services;
using NLog;
namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
[ServiceBinding(typeof(ICommandHandler<WithdrawFromVaultCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class WithdrawFromVaultCommandHandler : ICommandHandler<WithdrawFromVaultCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IVaultRepository _vaults;
    public WithdrawFromVaultCommandHandler(IVaultRepository vaults)
    {
        _vaults = vaults ?? throw new ArgumentNullException(nameof(vaults));
    }
    public async Task<CommandResult> HandleAsync(
        WithdrawFromVaultCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            int withdrawn = await _vaults.WithdrawAsync(
                command.Owner.Value,
                command.AreaResRef,
                command.RequestedAmount,
                cancellationToken);
            if (withdrawn == 0)
            {
                return CommandResult.Fail("No funds available to withdraw from vault.");
            }
            Log.Info("Withdrew {Withdrawn} gp from vault for owner {Owner} in area {Area} (requested: {Requested})",
                withdrawn, command.Owner, command.AreaResRef, command.RequestedAmount);
            return CommandResult.OkWith("withdrawnAmount", withdrawn);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to withdraw from vault for owner {Owner} in area {Area}",
                command.Owner, command.AreaResRef);
            return CommandResult.Fail($"Failed to withdraw from vault: {ex.Message}");
        }
    }
}
