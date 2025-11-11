using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

public interface INpcShopItemFactory
{
    Task<NwItem?> CreateForInventoryAsync(
        NwCreature owner,
        NpcShopProduct product,
        ConsignedItemData? consignedItem = null,
        CancellationToken cancellationToken = default);
}
