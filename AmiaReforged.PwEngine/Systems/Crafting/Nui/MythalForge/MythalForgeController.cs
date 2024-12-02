using AmiaReforged.PwEngine.Systems.WindowingSystem;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public class MythalForgeController : NuiController<MythalForgeView>
{

    public override void Init()
    {
        Token.Player.OnPlayerTarget += ValidateAndSelect;
        Token.Player.OnClientLeave += Unsubscribe;
    }

    private void Unsubscribe(ModuleEvents.OnClientLeave obj)
    {
        if (obj.Player != Token.Player) return;

        Token.Player.OnPlayerTarget -= ValidateAndSelect;
        Token.Player.OnClientLeave -= Unsubscribe;
    }

    private void ValidateAndSelect(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject is NwItem item)
        {
            Token.Player.SendServerMessage($"You selected {item.Name}");
        }
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
        if (eventData.ElementId == View.SelectItemButton.Id)
        {
            EnterTargetingMode();
        }
    }

    private void EnterTargetingMode()
    {
        NWScript.EnterTargetingMode(Token.Player.LoginCreature, NWScript.OBJECT_TYPE_ITEM);
    }

    protected override void OnClose()
    {
        Token.Player.OnPlayerTarget -= ValidateAndSelect;
    }
}