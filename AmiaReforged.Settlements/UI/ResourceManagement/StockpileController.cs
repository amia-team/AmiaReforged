using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Core.Services.Settlements;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.Settlements.Services.ResourceManagement;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Settlements.UI.ResourceManagement;

public class StockpileController : WindowController<StockpileView>
{
    [Inject] private Lazy<SettlementDataService> StockpileDataService { get; set; }
    private List<StockpiledItem> _stockpileItems;
    private List<StockpileUser> _authorizedUsers;
    
    public override async void Init()
    {
        NwPlayer player = Token.Player;
        int stockpileId = NWScript.GetLocalInt(player.LoginCreature, StockpileConstants.SettlementIdLocalVariable);

        Settlement? settlement = await StockpileDataService.Value.GetSettlementById(stockpileId);

        await NwTask.SwitchToMainThread();
        
        if (settlement == null)
        {
            player.SendServerMessage("This stockpile is not associated with a settlement or organization.", ColorConstants.Red);
            Token.Close();
            return;
        }
        
        _stockpileItems = settlement.Stockpile.ItemData;
        
        PopulateStockpileItems();
    }

    private void PopulateStockpileItems()
    {
        
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
    }

    protected override void OnClose()
    {
    }
}