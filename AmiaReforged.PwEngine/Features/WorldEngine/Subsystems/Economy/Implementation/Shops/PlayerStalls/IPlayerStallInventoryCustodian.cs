using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

/// <summary>
/// Handles sequestration of stall inventory when the lease lapses.
/// </summary>
public interface IPlayerStallInventoryCustodian
{
    Task TransferInventoryToMarketReeveAsync(PlayerStall stall, CancellationToken cancellationToken = default);
}
