using System.Threading;
using System.Threading.Tasks;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public interface INpcShopItemFactory
{
    Task<NwItem?> CreateForInventoryAsync(
        NwCreature owner,
        NpcShopProduct product,
        ConsignedItemData? consignedItem = null,
        CancellationToken cancellationToken = default);
}
