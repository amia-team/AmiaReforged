using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.Mercantile;

[ServiceBinding(typeof(PlayerShopEventBus))]
public class PlayerShopEventBus
{

    private List<PlayerShopInstance> _shopInstances = [];

    public bool Observe(PlayerShopInstance instance)
    {
        return false;
    }
}
