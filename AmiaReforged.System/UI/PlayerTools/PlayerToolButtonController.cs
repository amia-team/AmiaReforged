using System.Numerics;
using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools;

public class PlayerToolButtonController : WindowController<PlayerToolButtonView>
{
    [Inject] private Lazy<WindowManager> WindowManager { get; init; }

    public override void Init()
    {
        Vector2 windowPosition = new Vector2(725f, 0f);

        Token.SetBindValue(View.ButtonGeometry, new NuiRect(windowPosition.X, windowPosition.Y, 260f, 120f));
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

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == View.Button.Id)
        {
            string guid = PcKeyUtils.GetPcKey(eventData.Player).ToString();

            if (guid == Guid.Empty.ToString())
            {
                eventData.Player.SendServerMessage(
                    "Could not source your character, so functionality may be limited. If you don't have a PC key, you'll need to enter the travel agency and try again.");
            }
            else
            {
                NWScript.SetLocalString(eventData.Player.LoginCreature, "pc_guid", guid);
            }

            WindowManager.Value.OpenWindow<PlayerToolsWindowView>(Token.Player);
        }
    }

    protected override void OnClose()
    {
        // Do nothing
    }
}