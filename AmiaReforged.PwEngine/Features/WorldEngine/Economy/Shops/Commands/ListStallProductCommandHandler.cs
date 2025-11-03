using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Commands;

/// <summary>
/// Handles listing an item for sale on a player stall.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<ListStallProductCommand>))]
public sealed class ListStallProductCommandHandler : ICommandHandler<ListStallProductCommand>
{
    private readonly IPlayerShopRepository _shops;

    public ListStallProductCommandHandler(IPlayerShopRepository shops)
    {
        _shops = shops;
    }

    public Task<CommandResult> HandleAsync(
        ListStallProductCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PlayerStall? stall = _shops.GetShopById(command.StallId);
        if (stall == null)
        {
            return Task.FromResult(CommandResult.Fail($"Stall {command.StallId} was not found."));
        }

        StallProduct product = new StallProduct
        {
            StallId = command.StallId,
            ResRef = command.ResRef,
            Name = command.Name,
            Description = command.Description,
            Price = command.Price,
            Quantity = command.Quantity,
            BaseItemType = command.BaseItemType,
            ItemData = (byte[])command.ItemData.Clone(),
            ConsignedByPersonaId = command.ConsignorPersona?.ToString(),
            ConsignedByDisplayName = command.ConsignorDisplayName,
            Notes = command.Notes,
            SortOrder = command.SortOrder,
            IsActive = command.IsActive,
            ListedUtc = command.ListedUtc,
            UpdatedUtc = command.UpdatedUtc
        };

        _shops.AddProductToShop(command.StallId, product);

        return Task.FromResult(CommandResult.OkWith("productId", product.Id));
    }
}
