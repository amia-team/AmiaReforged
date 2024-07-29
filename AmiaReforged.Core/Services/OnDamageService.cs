using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(OnDamageService))]
public class OnDamageService
{

    public OnDamageService()
    {
        SubscribeNwnxDamage();
    }

    private void SubscribeNwnxDamage()
    {
    }
}