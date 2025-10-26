using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.Mercantile;

[ServiceBinding(typeof(PlayerShopService))]
public class PlayerShopService
{
    private const string PlayerShopResRef = "pw_player_shop";

    public PlayerShopService()
    {

    }
}
