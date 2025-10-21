using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Crafting;

// ... existing code ...
[ServiceBinding(typeof(MythalForgeInitializer))]
public class MythalForgeInitializer
{
    private const string TargetingModeMythalForge = MythalForgeTag;
    private const string LvarTargetingMode = "targeting_mode";
    private const string CanUseForge = "CAN_USE_FORGE";
    private const string ForgeIsClosing = "CLOSING_FORGE";

    private const string MythalForgeTag = "mythal_forge";
    private const string MythalTriggerTag = "mythal_trigger";
    private readonly CraftingBudgetService _budget;
    private readonly DifficultyClassCalculator _dcCalculator;
    private readonly CraftingPropertyData _propertyData;
    private readonly PropertyValidator _validator;

    private readonly WindowDirector _windowSystem;

    // ... existing code ...
    public MythalForgeInitializer(WindowDirector windowSystem, CraftingPropertyData propertyData,
        CraftingBudgetService budget, PropertyValidator validator, DifficultyClassCalculator dcCalculator)
    {
        _windowSystem = windowSystem;
        _propertyData = propertyData;
        _budget = budget;
        _validator = validator;
        _dcCalculator = dcCalculator;

        InitForges();
    }

    // ... existing code ...
    private void InitForges()
    {
        IEnumerable<NwPlaceable> forges = NwObject.FindObjectsWithTag<NwPlaceable>(MythalForgeTag);

        foreach (NwPlaceable nwPlaceable in forges)
        {
            nwPlaceable.OnUsed += OpenForge;
        }

        IEnumerable<NwTrigger> forgeTriggers = NwObject.FindObjectsWithTag<NwTrigger>(MythalTriggerTag);

        foreach (NwTrigger trigger in forgeTriggers)
        {
            trigger.OnEnter += EnableForgeUse;
            trigger.OnExit += DisableForgeUse;
        }
    }

    // ... existing code ...
    private void EnableForgeUse(TriggerEvents.OnEnter obj)
    {
        if (!obj.EnteringObject.IsLoginPlayerCharacter(out NwPlayer? player)) return;
        NwCreature? character = player.LoginCreature;
        if (character == null) return;

        NWScript.SetLocalInt(character, CanUseForge, 1);
    }

    // ... existing code ...
    private void DisableForgeUse(TriggerEvents.OnExit obj)
    {
        if (!obj.ExitingObject.IsLoginPlayerCharacter(out NwPlayer? player)) return;

        NwCreature? character = player.LoginCreature;
        if (character == null) return;

        NWScript.DeleteLocalInt(character, CanUseForge);
        NWScript.SetLocalInt(character, ForgeIsClosing, 1);
        if (_windowSystem.IsWindowOpen(player, typeof(MythalForgePresenter)))
            _windowSystem.CloseWindow(player, typeof(MythalForgePresenter));

        NWScript.DeleteLocalInt(character, ForgeIsClosing);
    }

    // ... existing code ...
    private void OpenForge(PlaceableEvents.OnUsed obj)
    {
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;

        // Toggle close if already open
        if (_windowSystem.IsWindowOpen(player, typeof(MythalForgePresenter)))
        {
            _windowSystem.CloseWindow(player, typeof(MythalForgePresenter));
            return;
        }

        NwCreature? character = player.LoginCreature;
        if (character == null) return;

        // DM avatar path: bypass triggers/lvars and allow picking any item (including outside inventory).
        if (character.IsDMAvatar)
        {
            player.OnPlayerTarget += ValidateAndSelectAsDM;
            EnterTargetingModeForDM(player);
            return;
        }

        // Player path (unchanged proximity/closing checks)
        if (NWScript.GetLocalInt(character, CanUseForge) != 1 || NWScript.GetLocalInt(character, ForgeIsClosing) == 1)
        {
            player.FloatingTextString(message: "Get closer to the forge.", false);
            return;
        }

        player.OnPlayerTarget += ValidateAndSelectPlayer;

        EnterTargetingMode(player);

        NWScript.SetLocalString(player.LoginCreature, LvarTargetingMode, TargetingModeMythalForge);
    }

    // ... existing code ...
    private void EnterTargetingMode(NwPlayer player)
    {
        player.FloatingTextString(message: "Pick an Item from your inventory.", false);
        player.OpenInventory();
        NWScript.SetLocalString(player.LoginCreature, LvarTargetingMode, TargetingModeMythalForge);
        NWScript.EnterTargetingMode(player.LoginCreature, NWScript.OBJECT_TYPE_ITEM);
    }

    // DM targeting mode: no lvars, allow selecting any item
    private void EnterTargetingModeForDM(NwPlayer player)
    {
        player.FloatingTextString(message: "DM: Select any item.", false);
        // Allow targeting of any item (not constrained by inventory token)
        NWScript.EnterTargetingMode(player.LoginCreature!, NWScript.OBJECT_TYPE_ITEM);
    }

    // Player validator (original behavior)
    private void ValidateAndSelectPlayer(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject is not NwItem item || !obj.TargetObject.IsValid) return;
        if (obj.Player.LoginCreature == null) return;

        if (NWScript.GetLocalString(obj.Player.LoginCreature, LvarTargetingMode) != TargetingModeMythalForge) return;
        int baseItemType = NWScript.GetBaseItemType(item);

        bool isTwoHander = ItemTypeConstants.Melee2HWeapons().Contains(baseItemType);
        bool isCasterWeapon = NWScript.GetLocalInt(item, sVarName: "CASTER_WEAPON") == NWScript.TRUE;
        if (isCasterWeapon)
            baseItemType = isTwoHander ? CraftingPropertyData.CasterWeapon2H : CraftingPropertyData.CasterWeapon1H;

        bool itemListingNotFound =
            !_propertyData.Properties.TryGetValue(baseItemType, out IReadOnlyList<CraftingCategory>? categories);
        if (itemListingNotFound)
        {
            GenericWindow.Builder()
                .For()
                .SimplePopup()
                .WithPlayer(obj.Player)
                .WithTitle(title: "Mythal Forge: Notice")
                .WithMessage(message: "Item not supported by Mythal forge")
                .Open();

            obj.Player.OnPlayerTarget -= ValidateAndSelectPlayer;

            NWScript.DeleteLocalString(obj.Player.LoginCreature, LvarTargetingMode);

            obj.Player.OpenInventory();

            return;
        }

        if (item.Possessor != null && item.Possessor.ObjectId != obj.Player.LoginCreature.ObjectId)
        {
            GenericWindow.Builder()
                .For()
                .SimplePopup()
                .WithPlayer(obj.Player)
                .WithTitle(title: "Mythal Forge: Notice")
                .WithMessage(message: "That doesn't belong to you. Pick an item from your inventory.")
                .Open();
            obj.Player.OpenInventory();
            return;
        }

        if (categories == null)
        {
            GenericWindow.Builder()
                .For()
                .SimplePopup()
                .WithPlayer(obj.Player)
                .WithTitle(title: "Mythal Forge: Error")
                .WithMessage(
                    message:
                    "Item supported by the Mythal forge, but has no properties. This is a bug and should be reported.")
                .Open();

            obj.Player.OnPlayerTarget -= ValidateAndSelectPlayer;
            NWScript.DeleteLocalString(obj.Player.LoginCreature, LvarTargetingMode);

            return;
        }

        NWScript.DeleteLocalString(obj.Player.LoginCreature, LvarTargetingMode);

        MythalForgeView itemWindow = new(_propertyData, _budget, item, obj.Player, _validator, _dcCalculator);
        _windowSystem.OpenWindow(itemWindow.Presenter);

        obj.Player.OpenInventory();
        obj.Player.OnPlayerTarget -= ValidateAndSelectPlayer;
    }

    // DM validator: no lvar or proximity checks; no ownership restriction
    private void ValidateAndSelectAsDM(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject is not NwItem item || !obj.TargetObject.IsValid) return;
        if (obj.Player.LoginCreature == null) return;

        int baseItemType = NWScript.GetBaseItemType(item);
        bool isTwoHander = ItemTypeConstants.Melee2HWeapons().Contains(baseItemType);
        bool isCasterWeapon = NWScript.GetLocalInt(item, sVarName: "CASTER_WEAPON") == NWScript.TRUE;
        if (isCasterWeapon)
            baseItemType = isTwoHander ? CraftingPropertyData.CasterWeapon2H : CraftingPropertyData.CasterWeapon1H;

        if (!_propertyData.Properties.ContainsKey(baseItemType))
        {
            GenericWindow.Builder()
                .For()
                .SimplePopup()
                .WithPlayer(obj.Player)
                .WithTitle("DM Forge: Notice")
                .WithMessage("Item base type not mapped for forge properties.")
                .Open();

            obj.Player.OnPlayerTarget -= ValidateAndSelectAsDM;
            return;
        }

        // Open DM Forge (no costs, no mythals, duplicates allowed)
        DmForgePresenter dmPresenter = new(obj.Player, item, _propertyData);
        _windowSystem.OpenWindow(dmPresenter);

        obj.Player.OnPlayerTarget -= ValidateAndSelectAsDM;
    }
}
