using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Commands;

/// <summary>
/// Handles releasing a stall from its current owner back to the system pool.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<ReleasePlayerStallCommand>))]
public sealed class ReleasePlayerStallCommandHandler : ICommandHandler<ReleasePlayerStallCommand>
{
    private readonly IPlayerStallService _stallService;

    public ReleasePlayerStallCommandHandler(IPlayerStallService stallService)
    {
        _stallService = stallService ?? throw new ArgumentNullException(nameof(stallService));
    }

    public Task<CommandResult> HandleAsync(
        ReleasePlayerStallCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ReleasePlayerStallRequest request = new(
            command.StallId,
            command.Requestor,
            command.Force);

        return _stallService.ReleaseAsync(request, cancellationToken)
            .ContinueWith(task => MapToCommandResult(task.Result), cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private static CommandResult MapToCommandResult(PlayerStallServiceResult serviceResult)
    {
        if (serviceResult.Success)
        {
            return CommandResult.Ok(CopyPayload(serviceResult.Data));
        }

        return CommandResult.Fail(serviceResult.ErrorMessage ?? "Failed to release stall.");
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
