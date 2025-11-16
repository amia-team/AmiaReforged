using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Treasuries;
using Anvil.Services;
using NLog;
namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
[ServiceBinding(typeof(ICommandHandler<DepositToVaultCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class DepositToVaultCommandHandler : ICommandHandler<DepositToVaultCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IVaultRepository _vaults;
    public DepositToVaultCommandHandler(IVaultRepository vaults)
    {
        _vaults = vaults ?? throw new ArgumentNullException(nameof(vaults));
    }
    public async Task<CommandResult> HandleAsync(
        DepositToVaultCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            int newBalance = await _vaults.DepositAsync(
                command.Owner.Value,
                command.AreaResRef,
                command.Amount,
                cancellationToken);
            Log.Info("Deposited {Amount} gp to vault for owner {Owner} in area {Area}. New balance: {Balance}",
                command.Amount, command.Owner, command.AreaResRef, newBalance);
            return CommandResult.OkWith("newBalance", newBalance);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to deposit {Amount} gp to vault for owner {Owner} in area {Area}",
                command.Amount, command.Owner, command.AreaResRef);
            return CommandResult.Fail($"Failed to deposit to vault: {ex.Message}");
        }
    }
}
