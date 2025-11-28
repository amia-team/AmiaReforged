using System.Text;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.API;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

[ServiceBinding(typeof(INpcShopItemFactory))]
public sealed class NpcShopItemFactory : INpcShopItemFactory
{
    public async Task<NwItem?> CreateForInventoryAsync(
        NwCreature owner,
        NpcShopProduct product,
        ConsignedItemData? consignedItem = null,
        CancellationToken cancellationToken = default)
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

        if (consignedItem is not null)
        {
            string jsonText = Encoding.UTF8.GetString(consignedItem.ItemData);
            Json json = Json.Parse(jsonText);
            NwItem? restored = json.ToNwObject<NwItem>(location, owner);
            if (restored is null)
            {
                return null;
            }

            return restored;
        }

        string tagOverride = "";
        if (product.LocalVariables.Any(v => v is { Name: "tag_override", Type: JsonLocalVariableType.String }))
        {
            tagOverride = product.LocalVariables
                .First(v => v is { Name: "tag_override", Type: JsonLocalVariableType.String }).StringValue ?? "";
        }

        NwItem? item = NwItem.Create(product.ResRef, location);
        if (item is null)
        {
            return null;
        }

        if (!tagOverride.IsNullOrEmpty())
        {
            item.Tag = tagOverride;
        }

        if (!string.IsNullOrWhiteSpace(product.DisplayName))
        {
            item.Name = product.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(product.Description))
        {
            item.Description = product.Description;
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
