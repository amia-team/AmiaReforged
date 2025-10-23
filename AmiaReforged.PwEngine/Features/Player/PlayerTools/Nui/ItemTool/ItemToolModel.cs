using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.ItemTool;

public enum IconAdjustResult { Success, NotAllowedType, NoSelection }

internal sealed class ItemToolModel(NwPlayer player)
{
    public NwItem? Selected { get; private set; }
    public bool HasSelected => Selected != null;

    public delegate void NewSelectionHandler();
    public event NewSelectionHandler? OnNewSelection;

    // --- Selection ---
    public void EnterTargetingMode()
    {
        player.EnterTargetMode(OnTargetItem, new TargetModeSettings { ValidTargets = ObjectTypes.Item });
    }
    private static readonly HashSet<string> BarredResrefs = new(StringComparer.OrdinalIgnoreCase)
    {
        "ds_pckey",
        "mythal1",
        "mythal2",
        "mythal3",
        "mythal4",
        "mythal5",
        "mythal6",
        "mythal7",
        "char_template",
        "ass_customizer",
        "dd_grandfather",
        "goodboi",
        "platinum_token",
        "jobjournal",
        "cust_summon"
    };

    private void OnTargetItem(ModuleEvents.OnPlayerTarget ev)
    {
        if (ev.TargetObject is not NwItem item)
        {
            player.SendServerMessage("Please target a valid item.", ColorConstants.Orange);
            return;
        }

        if (BarredResrefs.Contains(item.ResRef))
        {
            player.SendServerMessage("You cannot modify this item. Please select a different item.", ColorConstants.Red);
            return;
        }

        Selected = item;
        OnNewSelection?.Invoke();
    }

    public void UpdateBasic(string name, string description)
    {
        if (Selected is null) return;
        Selected.Name = name;
        Selected.Description = description;
    }

    public bool IsIconAllowed(out int current, out int max)
    {
        current = 0; max = 0;
        if (Selected is null) return false;

        uint baseId = Selected.BaseItem.Id;
        if (!TryGetMaxForBaseType(Selected, baseId, out max))
            return false;

        current = NWScript.GetItemAppearance(Selected, (int)ItemAppearanceType.SimpleModel, 0);
        return true;
    }

    public IconAdjustResult TryAdjustIcon(int delta, out int newValue, out int maxValue)
    {
        newValue = 0; maxValue = 0;
        if (Selected is null) return IconAdjustResult.NoSelection;

        uint baseId = Selected.BaseItem.Id;
        if (!TryGetMaxForBaseType(Selected, baseId, out maxValue))
            return IconAdjustResult.NotAllowedType;

        int current = NWScript.GetItemAppearance(Selected, (int)ItemAppearanceType.SimpleModel, 0);
        int target  = current + delta;

        if (target < 1) target = 1;
        if (target > maxValue) target = maxValue;

        // Use CopyItemAndModify like your NWScript did; replace the old item if copy succeeds
        uint copy = NWScript.CopyItemAndModify(Selected, (int)ItemAppearanceType.SimpleModel, 0, target, 1);
        if (NWScript.GetIsObjectValid(copy) == 1)
        {
            // Replace references and destroy the original one
            NWScript.DestroyObject(Selected);
            Selected = copy.ToNwObject<NwItem>();
        }

        newValue = target;
        return IconAdjustResult.Success;
    }

    private static bool TryGetMaxForBaseType(NwItem item, uint baseId, out int max)
    {
        // Matches i_ds_item_portrt:
        //  - Misc Large -> 31
        //  - Misc Medium -> 254
        //  - Misc Medium 2 (id 121) -> 66
        //  - Misc Small -> 254
        //  - Misc Small 2 (id 119) -> 254
        //  - Misc Small 3 (id 120) -> 100
        //  - Misc Thin -> 101
        // Plus ALSO allow: AMULET, BELT, BOOK, BRACER, GEM, GLOVES, LARGEBOX, RING (use 254 as safe cap)
        max = 0;

        BaseItemType bi = item.BaseItem.ItemType;
        switch (bi)
        {
            case BaseItemType.MiscLarge:
                max = 31; return true;
            case BaseItemType.MiscMedium:
                max = 254; return true;
            case BaseItemType.MiscSmall:
                max = 254; return true;
            case BaseItemType.MiscThin:
                max = 101; return true;

            case BaseItemType.Amulet:
            case BaseItemType.Belt:
            case BaseItemType.Book:
            case BaseItemType.Bracer:
            case BaseItemType.Gem:
            case BaseItemType.Gloves:
            case BaseItemType.LargeBox:
            case BaseItemType.Ring:
            case BaseItemType.LargeShield:
            case BaseItemType.TowerShield:
            case BaseItemType.SmallShield:
                max = 254; return true;
        }

        // Fallback for oddball variants present in your script (small2/small3/med2 by numeric id)
        if (baseId == 121) { max = 66;  return true; }  // Misc Medium 2
        if (baseId == 119) { max = 254; return true; }  // Misc Small 2
        if (baseId == 120) { max = 100; return true; }  // Misc Small 3

        return false;
    }
}
