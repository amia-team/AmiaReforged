using System;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Sanitization;

[ServiceBinding(typeof(NpcShopWindowFlagSanitizer))]
public sealed class NpcShopWindowFlagSanitizer : ILocalVariableSanitizer
{
    private const string ShopWindowFlagPrefix = "engine_npc_shop_window_";

    private readonly INpcShopRepository _shops;

    public NpcShopWindowFlagSanitizer(
        ILocalVariableSanitizationService sanitizationService,
        INpcShopRepository shops)
    {
        _shops = shops;
        sanitizationService.RegisterSanitizer(this);
    }

    public string Name => "NPC shop window flags";

    public void Sanitize(NwCreature creature)
    {
        if (creature is not { IsValid: true })
        {
            return;
        }

        foreach (NpcShop shop in _shops.All())
        {
            if (string.IsNullOrWhiteSpace(shop.Tag))
            {
                continue;
            }

            string legacyKey = ShopWindowFlagPrefix + shop.Tag;
            string sanitizedKey = LocalVariableKeyUtility.BuildKey(ShopWindowFlagPrefix, shop.Tag);

            if (string.Equals(legacyKey, sanitizedKey, StringComparison.Ordinal))
            {
                continue;
            }

            LocalVariableInt legacyVariable = creature.GetObjectVariable<LocalVariableInt>(legacyKey);
            if (!legacyVariable.HasValue)
            {
                continue;
            }

            int value = legacyVariable.Value;
            creature.GetObjectVariable<LocalVariableInt>(sanitizedKey).Value = value;
            legacyVariable.Delete();
        }
    }
}
