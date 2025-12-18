using System.Text;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

/// <summary>
/// Recreates NWN items from serialized stall product payloads.
/// </summary>
internal static class StallProductRestorer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Restores an item from a stall product and delivers it to the specified owner.
    /// </summary>
    /// <param name="product">The stall product containing the serialized item data.</param>
    /// <param name="owner">The creature who will receive the item.</param>
    /// <param name="quantity">
    /// The quantity to deliver. For stackable items, this sets the stack size of the delivered item.
    /// Defaults to 1 to prevent accidental full-stack delivery when buying single units.
    /// </param>
    /// <returns>The restored item, or null if restoration failed.</returns>
    public static async Task<NwItem?> RestoreItemAsync(StallProduct product, NwCreature owner, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(owner);

        if (quantity < 1)
        {
            quantity = 1;
        }

        await NwTask.SwitchToMainThread();

        if (owner is not { IsValid: true })
        {
            return null;
        }

        Location? location = owner.Location;
        if (location is null)
        {
            return null;
        }

        try
        {
            string jsonText = Encoding.UTF8.GetString(product.ItemData);
            Json json = Json.Parse(jsonText);

            // Create the item on the ground first (not directly in inventory).
            // This prevents auto-stacking with existing inventory items before
            // we can set the correct stack size for partial purchases.
            NwItem? item = json.ToNwObject<NwItem>(location);

            if (item is not null && item.IsValid)
            {
                // Set the stack size to the purchased quantity while the item
                // is still on the ground, before transferring to inventory.
                item.StackSize = quantity;
                owner.AcquireItem(item);
            }

            return item;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to restore player stall item for product {ProductId}.", product.Id);
            return null;
        }
    }
}
