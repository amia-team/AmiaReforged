using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Commands;

/// <summary>
/// Handles listing an item for sale on a player stall.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<ListStallProductCommand>))]
public sealed class ListStallProductCommandHandler : ICommandHandler<ListStallProductCommand>
{
    private readonly IPlayerStallService _stallService;

    public ListStallProductCommandHandler(IPlayerStallService stallService)
    {
        _stallService = stallService ?? throw new ArgumentNullException(nameof(stallService));
    }

    public Task<CommandResult> HandleAsync(
        ListStallProductCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ListStallProductRequest request = new(
            command.StallId,
            command.ResRef,
            command.Name,
            command.Description,
            command.Price,
            command.Quantity,
            command.BaseItemType,
            command.ItemData,
            command.ConsignorPersona,
            command.ConsignorDisplayName,
            command.Notes,
            command.SortOrder,
            command.IsActive,
            command.ListedUtc,
            command.UpdatedUtc);

        return _stallService.ListProductAsync(request, cancellationToken)
            .ContinueWith(task => MapToCommandResult(task.Result), cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private static CommandResult MapToCommandResult(PlayerStallServiceResult serviceResult)
    {
        if (serviceResult.Success)
        {
            return CommandResult.Ok(CopyPayload(serviceResult.Data));
        }

        return CommandResult.Fail(serviceResult.ErrorMessage ?? "Failed to list product.");
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
