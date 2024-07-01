using AmiaReforged.Core.Services;
using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.System.UI.PlayerTools.Quickslots.CreateQuickslots;

public class CreateQuickslotsController : WindowController<CreateQuickslotsView>
{
    [Inject] private Lazy<QuickslotLoader> QuickslotLoader { get; set; }
    [Inject] private Lazy<WindowManager> WindowManager { get; set; }

    public override void Init()
    {
        Token.SetBindValue(View.QuickslotName, string.Empty);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(eventData);
                break;
        }
    }

    private async void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == View.CreateButton.Id)
        {
            await SaveQuickslots();
            await NwTask.SwitchToMainThread();
        }
        else if (eventData.ElementId == View.CancelButton.Id)
        {
            Token.Close();
        }
    }

    private async Task SaveQuickslots()
    {
        NwPlayer tokenPlayer = Token.Player;
        Guid playerId = PcKeyUtils.GetPcKey(tokenPlayer);
        byte[] serializedQuickbar = tokenPlayer.LoginCreature!.SerializeQuickbar()!;
        
        if (Token.GetBindValue(View.QuickslotName) == string.Empty)
        {
            tokenPlayer.SendServerMessage("You must enter a name for the quickslot.", ColorConstants.Red);
            return;
        }

        await QuickslotLoader.Value.SavePlayerQuickslots(Token.GetBindValue(View.QuickslotName)!, serializedQuickbar, playerId);
        await NwTask.SwitchToMainThread();

        Token.Close();
    }

    protected override void OnClose()
    {
        Token.SetBindValue(View.QuickslotName, string.Empty);
    }
}