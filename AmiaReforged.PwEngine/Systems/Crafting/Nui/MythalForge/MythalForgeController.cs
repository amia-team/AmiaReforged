using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.WindowingSystem;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public class MythalForgeController : NuiController<MythalForgeView>
{
    [Inject] private Lazy<CraftingPropertyData>? PropertyData { get; set; }

    private IReadOnlyList<CraftingPropertyCategory> _itemProperties;

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
        if (obj.TargetObject is not NwItem item || !obj.TargetObject.IsValid) return;

        int baseItemType = NWScript.GetBaseItemType(item);
        
        if (PropertyData is null)
        {
            //notify the player that the system is not available
            Token.Player.SendServerMessage(
                "The Mythal Forge is currently unavailable. This is very likely a bug and should be reported.");
            return;
        }

        PopulateData(baseItemType);
    }

    private void PopulateData(int baseItemType)
    {
        try
        {
            if (PropertyData != null) _itemProperties = PropertyData.Value.Properties[baseItemType];
            
            foreach (string se in _itemProperties.Select(g => g.Label))
            {
                Token.Player.SendServerMessage(se);
            }
        }
        catch (KeyNotFoundException)
        {
            Token.Player.SendServerMessage("This item cannot be used in the Mythal Forge.");
        }

        IEnumerable<string> labels = _itemProperties!.Select(p => p.Label).ToArray();
        
        Token.SetBindValues(View.PropertyCategories, labels);
        Token.SetBindValue(View.PropertyCount, labels.Count());
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
        Token.Player.SendServerMessage("Select an item to craft.");
        NWScript.EnterTargetingMode(Token.Player.LoginCreature, NWScript.OBJECT_TYPE_ITEM);
    }

    protected override void OnClose()
    {
        Token.Player.OnPlayerTarget -= ValidateAndSelect;
    }
}