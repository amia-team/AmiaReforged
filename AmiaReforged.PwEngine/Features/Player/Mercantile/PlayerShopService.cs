using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.Mercantile;

[ServiceBinding(typeof(PlayerShopService))]
public class PlayerShopService
{
    private const string ShopResRef = "player_shop";

    public PlayerShopService()
    {
        List<NwPlaceable> playerShops =
            NwObject.FindObjectsOfType<NwPlaceable>().Where(p => p.ResRef == ShopResRef).ToList();
    }
}

public class PlayerShop
{
    public bool CanAfford(NwCreature creature, int amount)
    {
        // Placeholder implementation
        return true;
    }
}

public record ItemId(long Value);

public record ShopItem(ItemId ItemId, string Name, string Description, int Cost);
