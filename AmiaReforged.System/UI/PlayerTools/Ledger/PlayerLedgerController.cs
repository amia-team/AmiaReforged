using AmiaReforged.Core.UserInterface;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.System.UI.PlayerTools.Ledger;

public class PlayerLedgerController : WindowController<PlayerLedgerView>
{
    
    public override void Init() 
    {
        PlayerJobLedger playerJobLedger = new PlayerJobLedger(Token.Player);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
    }

    protected override void OnClose()
    {
    }
}