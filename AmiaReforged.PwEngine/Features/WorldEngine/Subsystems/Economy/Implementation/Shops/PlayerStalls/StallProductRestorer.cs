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

    public static async Task<NwItem?> RestoreItemAsync(StallProduct product, NwCreature owner)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(owner);

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
            return json.ToNwObject<NwItem>(location, owner);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to restore player stall item for product {ProductId}.", product.Id);
            return null;
        }
    }
}
