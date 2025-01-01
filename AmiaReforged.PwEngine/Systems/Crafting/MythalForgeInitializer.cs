using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;
using AmiaReforged.PwEngine.Systems.WindowingSystem;
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

    private readonly NuiManager _windowManager;
    private readonly CraftingPropertyData _propertyData;
    private readonly ActiveCraftingData _activeCraftingData;
    private readonly CraftingWindowManager _craftingWindowManager;

    public MythalForgeInitializer(NuiManager windowManager, CraftingPropertyData propertyData, ActiveCraftingData activeCraftingData, CraftingWindowManager craftingWindowManager)
    {
        _windowManager = windowManager;
        _propertyData = propertyData;
        _activeCraftingData = activeCraftingData;
        _craftingWindowManager = craftingWindowManager;

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

        if (_windowManager.WindowIsOpen(player, typeof(MythalForgeController)))
        {
            player.SendServerMessage(
                "You already have the Mythal Forge open. Close it and select another item if you want to craft something else.",
                ColorConstants.Red);
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

            return;
        }
        
        if(categories == null)
        {
            obj.Player.SendServerMessage("Item supported by the Mythal forge, but has no properties. This is a bug and should be reported.", ColorConstants.Red);
            
            obj.Player.OnPlayerTarget -= ValidateAndSelect;

            return;
        }


        
        // PopulateData(baseItemType);
        // CalculateBudget(item);
        _activeCraftingData.SetSelectedCategory(obj.Player, categories);
        _activeCraftingData.SetSelectedItem(obj.Player, item);
        
        // Remove the token.
        NWScript.DeleteLocalString(obj.Player.LoginCreature, LvarTargetingMode);
        
        _craftingWindowManager.OpenWindow(obj.Player, item);
        
        obj.Player.OnPlayerTarget -= ValidateAndSelect;
    }
}