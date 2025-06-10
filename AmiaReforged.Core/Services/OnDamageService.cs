using Anvil.Services;

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