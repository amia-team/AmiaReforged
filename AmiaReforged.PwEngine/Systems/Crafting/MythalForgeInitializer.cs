using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;
using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.Crafting;

[ServiceBinding(typeof(MythalForgeInitializer))]
public class MythalForgeInitializer
{
    private const string TargetingModeMythalForge = "mythal_forge";
    private const string LvarTargetingMode = "targeting_mode";

    private readonly WindowDirector _windowSystem;
    private readonly CraftingPropertyData _propertyData;
    private readonly CraftingBudgetService _budget;

    public MythalForgeInitializer(WindowDirector windowSystem, CraftingPropertyData propertyData, CraftingBudgetService budget)
    {
        _windowSystem = windowSystem;
        _propertyData = propertyData;
        _budget = budget;

        InitForges();
    }

    private void InitForges()
    {
        IEnumerable<NwPlaceable> forges = NwObject.FindObjectsWithTag<NwPlaceable>("mythal_forge");

        foreach (NwPlaceable nwPlaceable in forges)
        {
            nwPlaceable.OnUsed += OpenForge;
        }
    }

    private void OpenForge(PlaceableEvents.OnUsed obj)
    {
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;

        if (_windowSystem.IsWindowOpen(player, typeof(MythalForgeWindow)))
        {
            _windowSystem.CloseWindow(player, typeof(MythalForgeWindow));
            return;
        }
       
        player.OnPlayerTarget += ValidateAndSelect;
        
        EnterTargetingMode(player);
        
        NWScript.SetLocalString(player.LoginCreature, LvarTargetingMode, TargetingModeMythalForge);
    }
    
    private void EnterTargetingMode(NwPlayer player)
    {
        player.FloatingTextString("Pick an Item from your inventory.", false);
        player.OpenInventory();
        NWScript.EnterTargetingMode(player.LoginCreature, NWScript.OBJECT_TYPE_ITEM);
        NWScript.SetLocalString(player.LoginCreature, LvarTargetingMode, TargetingModeMythalForge);
    }

    private void ValidateAndSelect(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject is not NwItem item || !obj.TargetObject.IsValid) return;
        if(obj.Player.LoginCreature == null) return;
        
        if(NWScript.GetLocalString(obj.Player.LoginCreature, LvarTargetingMode) != TargetingModeMythalForge) return;

        int baseItemType = NWScript.GetBaseItemType(item);

        bool notFound = !_propertyData.Properties.TryGetValue(baseItemType, out IReadOnlyList<CraftingCategory>? categories);
        if (notFound)
        {
            obj.Player.SendServerMessage("Item not supported by Mythal forge", ColorConstants.Orange);

            obj.Player.OnPlayerTarget -= ValidateAndSelect;
            
            NWScript.DeleteLocalString(obj.Player.LoginCreature, LvarTargetingMode);

            return;
        }
        
        if(categories == null)
        {
            obj.Player.SendServerMessage("Item supported by the Mythal forge, but has no properties. This is a bug and should be reported.", ColorConstants.Red);
            
            obj.Player.OnPlayerTarget -= ValidateAndSelect;
            NWScript.DeleteLocalString(obj.Player.LoginCreature, LvarTargetingMode);

            return;
        }
        
        
        // Remove the token.
        NWScript.DeleteLocalString(obj.Player.LoginCreature, LvarTargetingMode);
        
        MythalForgeView itemWindow = new MythalForgeView(_propertyData, _budget, item, obj.Player);
        _windowSystem.OpenWindow(itemWindow.Presenter);
        
        obj.Player.OnPlayerTarget -= ValidateAndSelect;
    }
}