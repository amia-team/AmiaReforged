using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using YamlDotNet.Serialization;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.ItemTool;

public enum IconAdjustResult { Success, NotAllowedType, NoSelection, NoValidModel }

internal sealed class ItemToolModel(NwPlayer player)
{
    public NwItem? Selected { get; private set; }
    public bool HasSelected => Selected != null;

    public delegate void NewSelectionHandler();
    public event NewSelectionHandler? OnNewSelection;

    private const string InitialNameKey = "item_tool_initial_name";
    private const string InitialDescKey = "item_tool_initial_desc";

    public void EnterTargetingMode()
    {
        player.EnterTargetMode(OnTargetItem, new TargetModeSettings { ValidTargets = ObjectTypes.Item});
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

        if (item.Possessor != player.ControlledCreature)
        {
            player.SendServerMessage("You can only modify items in your own inventory.", ColorConstants.Red);
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

    public void EnsureInitialNameCaptured()
    {
        if (Selected is null) return;
        LocalVariableString v = Selected.GetObjectVariable<LocalVariableString>(InitialNameKey);
        if (!v.HasValue) v.Value = Selected.Name;
    }
    public void EnsureInitialDescCaptured()
    {
        if (Selected is null) return;
        LocalVariableString v = Selected.GetObjectVariable<LocalVariableString>(InitialDescKey);
        if (!v.HasValue) v.Value = Selected.Description;
    }
    public string GetInitialNameOrCurrent()
    {
        if (Selected is null) return string.Empty;
        LocalVariableString v = Selected.GetObjectVariable<LocalVariableString>(InitialNameKey);
        return v.HasValue ? (v.Value ?? string.Empty) : Selected.Name;
    }
    public string GetInitialDescOrCurrent()
    {
        if (Selected is null) return string.Empty;
        LocalVariableString v = Selected.GetObjectVariable<LocalVariableString>(InitialDescKey);
        return v.HasValue ? (v.Value ?? string.Empty) : Selected.Description;
    }
    public void RevertNameToInitial()
    {
        if (Selected is null) return;
        LocalVariableString v = Selected.GetObjectVariable<LocalVariableString>(InitialNameKey);
        if (v.HasValue) Selected.Name = v.Value ?? Selected.Name;
    }
    public void RevertDescToInitial()
    {
        if (Selected is null) return;
        LocalVariableString v = Selected.GetObjectVariable<LocalVariableString>(InitialDescKey);
        if (v.HasValue) Selected.Description = v.Value ?? Selected.Description;
    }
    public void ClearInitials()
    {
        if (Selected is null) return;
        LocalVariableString n = Selected.GetObjectVariable<LocalVariableString>(InitialNameKey);
        if (n.HasValue) n.Delete();
        LocalVariableString d = Selected.GetObjectVariable<LocalVariableString>(InitialDescKey);
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

        if (!ItemModelValidation.SupportsModelChanges(Selected))
            return false;

        max = ItemModelValidation.GetMaxModelIndex(Selected);
        if (max == 0) return false;

        current = NWScript.GetItemAppearance(Selected, (int)ItemAppearanceType.SimpleModel, 0);
        return true;
    }

    public IconAdjustResult TryAdjustIcon(int delta, out int newValue, out int maxValue)
    {
        newValue = 0; maxValue = 0;
        if (Selected is null) return IconAdjustResult.NoSelection;

        if (!ItemModelValidation.SupportsModelChanges(Selected))
            return IconAdjustResult.NotAllowedType;

        maxValue = ItemModelValidation.GetMaxModelIndex(Selected);
        if (maxValue == 0)
            return IconAdjustResult.NotAllowedType;

        int current = NWScript.GetItemAppearance(Selected, (int)ItemAppearanceType.SimpleModel, 0);
        if (delta == 0)
        {
            newValue = current;
            return IconAdjustResult.Success;
        }

        int candidate = GetNextValidItemModel(current, delta, maxValue);

        if (candidate == current)
        {
            return IconAdjustResult.NoValidModel;
        }

        NwCreature? creature = player.ControlledCreature;
        InventorySlot? equippedSlot = null;

        if (creature != null)
        {
            foreach (InventorySlot slot in Enum.GetValues<InventorySlot>())
            {
                if (creature.GetItemInSlot(slot) == Selected)
                {
                    equippedSlot = slot;
                    break;
                }
            }

            if (equippedSlot.HasValue)
            {
                creature.RunUnequip(Selected);
            }
        }

        uint copy = NWScript.CopyItemAndModify(Selected, (int)ItemAppearanceType.SimpleModel, 0, candidate, 1);
        if (NWScript.GetIsObjectValid(copy) == 1)
        {
            NWScript.DestroyObject(Selected);
            Selected = copy.ToNwObject<NwItem>();

            if (creature != null && equippedSlot.HasValue && Selected != null)
            {
                creature.RunEquip(Selected, equippedSlot.Value);
            }

            newValue = candidate;
            return IconAdjustResult.Success;
        }

        if (creature != null && equippedSlot.HasValue && Selected != null)
        {
            creature.RunEquip(Selected, equippedSlot.Value);
        }

        newValue = current;
        return IconAdjustResult.NoValidModel;
    }

    private int GetNextValidItemModel(int currentModel, int delta, int maxModel)
    {
        if (Selected == null || !Selected.IsValid) return currentModel;
        if (maxModel <= 0) return currentModel;

        int direction = Math.Sign(delta);
        int step = Math.Abs(delta);
        int searchModel = currentModel;
        int attemptsRemaining = maxModel + 1;

        while (attemptsRemaining > 0)
        {
            if (step == 1)
            {
                searchModel += direction;
            }
            else
            {
                searchModel += delta;
                step = 1;
            }

            if (searchModel > maxModel)
            {
                searchModel = 1;
            }
            else if (searchModel < 1)
            {
                searchModel = maxModel;
            }

            if (searchModel == currentModel && attemptsRemaining < maxModel)
            {
                return currentModel;
            }

            if (ItemModelValidation.IsValidModelIndex(Selected, searchModel))
            {
                return searchModel;
            }

            attemptsRemaining--;
        }

        return currentModel;
    }
}
