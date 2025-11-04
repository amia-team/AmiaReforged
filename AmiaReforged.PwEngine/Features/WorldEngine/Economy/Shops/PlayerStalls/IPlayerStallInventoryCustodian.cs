using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

/// <summary>
/// Handles sequestration of stall inventory when the lease lapses.
/// </summary>
public interface IPlayerStallInventoryCustodian
{
    Task TransferInventoryToMarketReeveAsync(PlayerStall stall, CancellationToken cancellationToken = default);
}
