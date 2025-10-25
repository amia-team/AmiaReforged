using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.ItemTool;

public enum IconAdjustResult { Success, NotAllowedType, NoSelection, NoValidModel }

internal sealed class ItemToolModel(NwPlayer player)
{
    public NwItem? Selected { get; private set; }
    public bool HasSelected => Selected != null;

    public delegate void NewSelectionHandler();
    public event NewSelectionHandler? OnNewSelection;

    // Local variable keys for initial values (persist across CopyItemAndModify)
    private const string InitialNameKey = "item_tool_initial_name";
    private const string InitialDescKey = "item_tool_initial_desc";

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

    // --- Initial value helpers ---
    public void EnsureInitialNameCaptured()
    {
        if (Selected is null) return;
        var v = Selected.GetObjectVariable<LocalVariableString>(InitialNameKey);
        if (!v.HasValue) v.Value = Selected.Name; // Name is non-null per annotations
    }
    public void EnsureInitialDescCaptured()
    {
        if (Selected is null) return;
        var v = Selected.GetObjectVariable<LocalVariableString>(InitialDescKey);
        if (!v.HasValue) v.Value = Selected.Description; // Description is non-null per annotations
    }
    public string GetInitialNameOrCurrent()
    {
        if (Selected is null) return string.Empty;
        var v = Selected.GetObjectVariable<LocalVariableString>(InitialNameKey);
        return v.HasValue ? (v.Value ?? string.Empty) : Selected.Name;
    }
    public string GetInitialDescOrCurrent()
    {
        if (Selected is null) return string.Empty;
        var v = Selected.GetObjectVariable<LocalVariableString>(InitialDescKey);
        return v.HasValue ? (v.Value ?? string.Empty) : Selected.Description;
    }
    public void RevertNameToInitial()
    {
        if (Selected is null) return;
        var v = Selected.GetObjectVariable<LocalVariableString>(InitialNameKey);
        if (v.HasValue) Selected.Name = v.Value ?? Selected.Name;
    }
    public void RevertDescToInitial()
    {
        if (Selected is null) return;
        var v = Selected.GetObjectVariable<LocalVariableString>(InitialDescKey);
        if (v.HasValue) Selected.Description = v.Value ?? Selected.Description;
    }
    public void ClearInitials()
    {
        if (Selected is null) return;
        var n = Selected.GetObjectVariable<LocalVariableString>(InitialNameKey);
        if (n.HasValue) n.Delete();
        var d = Selected.GetObjectVariable<LocalVariableString>(InitialDescKey);
        if (d.HasValue) d.Delete();
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
        if (delta == 0)
        {
            newValue = current;
            return IconAdjustResult.Success;
        }

        int dir = delta > 0 ? 1 : -1;

        // Try the requested delta first, then continue stepping (+/-1) wrapping within [1, maxValue]
        // This both wraps around and skips invalid targets (when CopyItemAndModify fails).
        int candidate = WrapToRange(current + delta, maxValue);

        // Attempt up to maxValue distinct candidates to find a valid model index
        int attempts = 0;
        while (attempts < maxValue)
        {
            uint copy = NWScript.CopyItemAndModify(Selected, (int)ItemAppearanceType.SimpleModel, 0, candidate, 1);
            if (NWScript.GetIsObjectValid(copy) == 1)
            {
                // Replace and return
                NWScript.DestroyObject(Selected);
                Selected = copy.ToNwObject<NwItem>();
                newValue = candidate;
                return IconAdjustResult.Success;
            }

            // Move candidate by 1 in the same direction of travel, wrapping
            candidate = WrapToRange(candidate + dir, maxValue);
            attempts++;
        }

        // No valid model found after scanning full space
        newValue = current;
        return IconAdjustResult.NoValidModel;
    }

    private static int WrapToRange(int value, int maxInclusive)
    {
        if (maxInclusive <= 0) return 1;
        // Wrap in 1..maxInclusive
        int n = (value - 1) % maxInclusive;
        if (n < 0) n += maxInclusive;
        return n + 1;
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
