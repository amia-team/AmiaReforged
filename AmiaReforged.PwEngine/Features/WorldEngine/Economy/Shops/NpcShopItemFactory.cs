using System;
using System.Threading;
using System.Threading.Tasks;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

[ServiceBinding(typeof(INpcShopItemFactory))]
public sealed class NpcShopItemFactory : INpcShopItemFactory
{
    public async Task<NwItem?> CreateForInventoryAsync(NwCreature owner, NpcShopProduct product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(product);

        if (!owner.IsValid)
        {
            return null;
        }

        await NwTask.SwitchToMainThread();

        cancellationToken.ThrowIfCancellationRequested();

        Location? location = owner.Location;
        if (location is null)
        {
            return null;
        }

        NwItem? item = NwItem.Create(product.ResRef, location);
        if (item is null)
        {
            return null;
        }

        NwItemLocalVariableWriter localWriter = new(item);
        foreach (NpcShopLocalVariable localVariable in product.LocalVariables)
        {
            localVariable.WriteTo(localWriter);
        }

        if (product.Appearance is { } appearance)
        {
            appearance.ApplyTo(new NwItemAppearanceWriter(item));
        }

        owner.AcquireItem(item);
        return item;
    }
}
