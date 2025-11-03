using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Commands;

/// <summary>
/// Handles requests to claim ownership of a player stall.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<ClaimPlayerStallCommand>))]
public sealed class ClaimPlayerStallCommandHandler : ICommandHandler<ClaimPlayerStallCommand>
{
    private readonly IPlayerStallService _stallService;

    public ClaimPlayerStallCommandHandler(IPlayerStallService stallService)
    {
        _stallService = stallService ?? throw new ArgumentNullException(nameof(stallService));
    }

    public Task<CommandResult> HandleAsync(
        ClaimPlayerStallCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ClaimPlayerStallRequest request = new(
            command.StallId,
            command.OwnerPersona,
            command.OwnerDisplayName,
            command.CoinHouseAccountId,
            command.HoldEarningsInStall,
            command.LeaseStartUtc,
            command.NextRentDueUtc);

        return _stallService.ClaimAsync(request, cancellationToken)
            .ContinueWith(task => MapToCommandResult(task.Result), cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private static CommandResult MapToCommandResult(PlayerStallServiceResult serviceResult)
    {
        if (serviceResult.Success)
        {
            return CommandResult.Ok(CopyPayload(serviceResult.Data));
        }

        return CommandResult.Fail(serviceResult.ErrorMessage ?? "Failed to claim stall.");
    }

    private static Dictionary<string, object>? CopyPayload(IReadOnlyDictionary<string, object>? data)
    {
        if (data is null)
        {
            return null;
        }

        Dictionary<string, object> copy = new(data.Count);
        foreach (KeyValuePair<string, object> entry in data)
        {
            copy[entry.Key] = entry.Value;
        }

        return copy;
    }
}
