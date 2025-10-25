using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core; // for NWScript interop

namespace AmiaReforged.PwEngine.Features.DungeonMaster.ItemEditor;

internal sealed class ItemEditorModel
{
    private readonly NwPlayer _player;

    public ItemEditorModel(NwPlayer player)
    {
        _player = player;
    }

    public NwItem? SelectedItem { get; private set; }

    public delegate void OnNewSelectionHandler();
    public event OnNewSelectionHandler? OnNewSelection;

    public void Update(ItemData data)
    {
        if (SelectedItem is null) return;
        if (ItemDataFactory.From(SelectedItem) == data) return;

        SelectedItem.Name = data.Name;
        SelectedItem.Description = data.Description;
        SelectedItem.Tag = data.Tag;

        // Update variables
        UpdateVariables(data.Variables);
    }

    private void UpdateVariables(Dictionary<string, LocalVariableData> newVariables)
    {
        if (SelectedItem is null) return;

        // Get current variables from item
        Dictionary<string, LocalVariableData> currentVars = GetCurrentVariables();

        // Remove variables that are no longer present
        foreach (string key in currentVars.Keys)
        {
            if (!newVariables.ContainsKey(key))
            {
                DeleteVariable(key);
            }
        }

        // Add or update variables
        foreach (KeyValuePair<string, LocalVariableData> kvp in newVariables)
        {
            SetVariable(kvp.Key, kvp.Value);
        }
    }

    private Dictionary<string, LocalVariableData> GetCurrentVariables()
    {
        Dictionary<string, LocalVariableData> variables = new();
        if (SelectedItem is null) return variables;

        foreach (ObjectVariable local in SelectedItem.LocalVariables)
        {
            switch (local)
            {
                case LocalVariableInt li:
                    variables[li.Name] = new LocalVariableData
                    {
                        Type = LocalVariableType.Int,
                        IntValue = li.Value
                    };
                    break;

                case LocalVariableFloat lf:
                    variables[lf.Name] = new LocalVariableData
                    {
                        Type = LocalVariableType.Float,
                        FloatValue = lf.Value
                    };
                    break;

                case LocalVariableString ls:
                    variables[ls.Name] = new LocalVariableData
                    {
                        Type = LocalVariableType.String,
                        StringValue = ls.Value ?? string.Empty
                    };
                    break;

                case LocalVariableLocation lloc:
                    variables[lloc.Name] = new LocalVariableData
                    {
                        Type = LocalVariableType.Location,
                        LocationValue = lloc.Value
                    };
                    break;

                case LocalVariableObject<NwObject> lo:
                    variables[lo.Name] = new LocalVariableData
                    {
                        Type = LocalVariableType.Object,
                        ObjectValue = lo.Value
                    };
                    break;
            }
        }

        return variables;
    }

    private void SetVariable(string name, LocalVariableData data)
    {
        if (SelectedItem is null) return;

        switch (data.Type)
        {
            case LocalVariableType.Int:
                SelectedItem.GetObjectVariable<LocalVariableInt>(name).Value = data.IntValue;
                break;
            case LocalVariableType.Float:
                SelectedItem.GetObjectVariable<LocalVariableFloat>(name).Value = data.FloatValue;
                break;
            case LocalVariableType.String:
                SelectedItem.GetObjectVariable<LocalVariableString>(name).Value = data.StringValue;
                break;
            case LocalVariableType.Location:
                SelectedItem.GetObjectVariable<LocalVariableLocation>(name).Value = data.LocationValue;
                break;
            case LocalVariableType.Object:
                SelectedItem.GetObjectVariable<LocalVariableObject<NwObject>>(name).Value = data.ObjectValue;
                break;
        }
    }

    private void DeleteVariable(string name)
    {
        if (SelectedItem is null) return;

        SelectedItem.GetObjectVariable<LocalVariableInt>(name).Delete();
        SelectedItem.GetObjectVariable<LocalVariableFloat>(name).Delete();
        SelectedItem.GetObjectVariable<LocalVariableString>(name).Delete();
        SelectedItem.GetObjectVariable<LocalVariableLocation>(name).Delete();
        SelectedItem.GetObjectVariable<LocalVariableObject<NwObject>>(name).Delete();
    }

    public void EnterTargetingMode()
    {
        _player.EnterTargetMode(StartItemSelection,
            new TargetModeSettings { ValidTargets = ObjectTypes.Item });
    }

    private void StartItemSelection(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject is NwItem item)
        {
            SelectedItem = item;
            OnNewSelection?.Invoke();
        }
        else
        {
            _player.SendServerMessage("Please target a valid item.", ColorConstants.Orange);
        }
    }

    // === Icon / Simple Model support (mirrors Player Item Tool) ===
    public bool IsIconAllowed(out int current, out int max)
    {
        current = 0; max = 0;
        if (SelectedItem is null) return false;

        uint baseId = SelectedItem.BaseItem.Id; // numeric fallback for oddball entries (119, 120, 121)
        if (!TryGetMaxForBaseType(SelectedItem, baseId, out max))
            return false;

        current = NWScript.GetItemAppearance(SelectedItem, (int)ItemAppearanceType.SimpleModel, 0);
        return true;
    }

    public IconAdjustResult TryAdjustIcon(int delta, out int newValue, out int maxValue)
    {
        newValue = 0; maxValue = 0;
        if (SelectedItem is null) return IconAdjustResult.NoSelection;

        uint baseId = SelectedItem.BaseItem.Id;
        if (!TryGetMaxForBaseType(SelectedItem, baseId, out maxValue))
            return IconAdjustResult.NotAllowedType;

        int current = NWScript.GetItemAppearance(SelectedItem, (int)ItemAppearanceType.SimpleModel, 0);
        int target  = current + delta;

        if (target < 1) target = 1;
        if (target > maxValue) target = maxValue;

        // Clone-and-replace so the appearance change persists (locals copy across)
        var copy = NWScript.CopyItemAndModify(SelectedItem, (int)ItemAppearanceType.SimpleModel, 0, target, 1);
        if (NWScript.GetIsObjectValid(copy) == 1)
        {
            NWScript.DestroyObject(SelectedItem);
            SelectedItem = copy.ToNwObject<NwItem>();
        }

        newValue = target;
        return IconAdjustResult.Success;
    }

    public enum IconAdjustResult { Success, NotAllowedType, NoSelection }

    private static bool TryGetMaxForBaseType(NwItem item, uint baseId, out int max)
    {
        // Allowed (same as player tool’s set):
        //  - Misc Large -> 31
        //  - Misc Medium -> 254
        //  - Misc Medium 2 (id 121) -> 66
        //  - Misc Small -> 254
        //  - Misc Small 2 (id 119) -> 254
        //  - Misc Small 3 (id 120) -> 100
        //  - Misc Thin -> 101
        // Plus ALSO allow: AMULET, BELT, BOOK, BRACER, GEM, GLOVES, LARGEBOX, RING, SHIELDS (use 254 as safe cap)

        max = 0;

        var bi = item.BaseItem.ItemType;
        switch (bi)
        {
            case BaseItemType.MiscLarge:  max = 254;  return true;
            case BaseItemType.MiscMedium: max = 254; return true;
            case BaseItemType.MiscSmall:  max = 254; return true;
            case BaseItemType.MiscThin:   max = 254; return true;

            case BaseItemType.Amulet:
            case BaseItemType.Belt:
            case BaseItemType.Book:
            case BaseItemType.Bracer:
            case BaseItemType.Gem:
            case BaseItemType.Gloves:
            case BaseItemType.LargeBox:
            case BaseItemType.Ring:
            case BaseItemType.SmallShield:
            case BaseItemType.TowerShield:
            case BaseItemType.LargeShield:
                max = 254; return true;
        }

        // Fallback for oddball variants present in legacy script (by numeric id)
        if (baseId == 121) { max = 254;  return true; }  // Misc Medium 2
        if (baseId == 119) { max = 254; return true; }  // Misc Small 2
        if (baseId == 120) { max = 254; return true; }  // Misc Small 3

        return false;
    }
}
