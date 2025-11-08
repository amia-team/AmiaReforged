using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage.Commands;

public class WithdrawItemCommandHandler : ICommandHandler<WithdrawItemCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwEngineContext _context;

    public WithdrawItemCommandHandler(PwEngineContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> HandleAsync(WithdrawItemCommand request, CancellationToken cancellationToken)
    {
        StoredItem? item = await _context.WarehouseItems
            .FirstOrDefaultAsync(
                i => i.Id == request.StoredItemId && i.Owner == request.CharacterId,
                cancellationToken);

        if (item == null)
        {
            Log.Warn($"Character {request.CharacterId} attempted to withdraw item {request.StoredItemId} " +
                     "but it was not found or not owned by them.");
            return CommandResult.Fail("Item not found or you don't have permission to withdraw it.");
        }

        byte[] itemData = item.ItemData;
        string itemName = item.Name;
        long warehouseId = item.WarehouseId;

        _context.WarehouseItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        Log.Info($"Character {request.CharacterId} withdrew item '{itemName}' (ID: {request.StoredItemId}) " +
                 $"from storage (Warehouse ID: {warehouseId}).");

        return CommandResult.Ok(new Dictionary<string, object>
        {
            ["ItemData"] = itemData,
            ["ItemName"] = itemName,
            ["Message"] = "Item withdrawn successfully."
        });
    }
}
