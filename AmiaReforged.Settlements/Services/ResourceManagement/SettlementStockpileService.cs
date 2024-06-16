using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.Settlements.UI;
using AmiaReforged.Settlements.UI.ResourceManagement;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Settlements.Services.ResourceManagement;

[ServiceBinding(typeof(SettlementStockpileService))]
public class SettlementStockpileService
{
    private readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly SettlementCacheService _cache;
    private readonly WindowManager _windowManager;
    public SettlementStockpileService(SettlementCacheService cache, WindowManager windowManager)
    {
        _cache = cache;
        _windowManager = windowManager;

        SubscribeStockpiles();
    }

    private void SubscribeStockpiles()
    {
        IEnumerable<NwObject> stockpileObjects = NwObject.FindObjectsWithTag(StockpileConstants.StockpileTag);

        foreach (NwObject o in stockpileObjects)
        {
            Log.Info("Subscribing to stockpile.");
            NwPlaceable stockpile = (NwPlaceable)o;
            stockpile.OnUsed += OnStockpileUsed;
        }
    }

    private void OnStockpileUsed(PlaceableEvents.OnUsed obj)
    {
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;
        if (player is not { IsValid: true, IsDM: false }) return;

        int stockpileId = NWScript.GetLocalInt(obj.Placeable, StockpileConstants.SettlementIdLocalVariable);
        NWScript.SetLocalInt(player.LoginCreature, StockpileConstants.SettlementIdLocalVariable, stockpileId);

        if (!_cache.Stockpiles.TryGetValue(stockpileId, out Stockpile? stockpile))
        {
            player.SendServerMessage("This stockpile is not associated with a settlement or organization.", ColorConstants.Red);
            return;
        }
        
        player.SendServerMessage("you clicked it :)");
        _windowManager.OpenWindow<StockpileView>(player);
    }
}