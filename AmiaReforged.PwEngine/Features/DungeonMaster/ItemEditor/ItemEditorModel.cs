using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core; // for NWScript interop
using AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.ItemTool;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.ItemEditor;

public enum IconAdjustResult { Success, NotAllowedType, NoSelection, NoValidModel }

internal sealed class ItemEditorModel
{
    private readonly NwPlayer _player;

    public ItemEditorModel(NwPlayer player)
    {
        _player = player;
    }

    public NwItem? SelectedItem { get; private set; }
    public bool HasSelected => SelectedItem != null;

    public delegate void OnNewSelectionHandler();
    public event OnNewSelectionHandler? OnNewSelection;

    // Local variable keys for initial values (persist across CopyItemAndModify)
    private const string InitialNameKey = "item_editor_initial_name";
    private const string InitialDescKey = "item_editor_initial_desc";

    // --- Initial value helpers ---
    public void EnsureInitialNameCaptured()
    {
        if (SelectedItem is null) return;
        LocalVariableString v = SelectedItem.GetObjectVariable<LocalVariableString>(InitialNameKey);
        if (!v.HasValue) v.Value = SelectedItem.Name;
    }

    public void EnsureInitialDescCaptured()
    {
        if (SelectedItem is null) return;
        LocalVariableString v = SelectedItem.GetObjectVariable<LocalVariableString>(InitialDescKey);
        if (!v.HasValue) v.Value = SelectedItem.Description;
    }

    public string GetInitialNameOrCurrent()
    {
        if (SelectedItem is null) return string.Empty;
        LocalVariableString v = SelectedItem.GetObjectVariable<LocalVariableString>(InitialNameKey);
        return v.HasValue ? (v.Value ?? string.Empty) : SelectedItem.Name;
    }

    public string GetInitialDescOrCurrent()
    {
        if (SelectedItem is null) return string.Empty;
        LocalVariableString v = SelectedItem.GetObjectVariable<LocalVariableString>(InitialDescKey);
        return v.HasValue ? (v.Value ?? string.Empty) : SelectedItem.Description;
    }

    public void RevertNameToInitial()
    {
        if (SelectedItem is null) return;
        LocalVariableString v = SelectedItem.GetObjectVariable<LocalVariableString>(InitialNameKey);
        if (v.HasValue) SelectedItem.Name = v.Value ?? SelectedItem.Name;
    }

    public void RevertDescToInitial()
    {
        if (SelectedItem is null) return;
        LocalVariableString v = SelectedItem.GetObjectVariable<LocalVariableString>(InitialDescKey);
        if (v.HasValue) SelectedItem.Description = v.Value ?? SelectedItem.Description;
    }

    public void ClearInitials()
    {
        if (SelectedItem is null) return;
        LocalVariableString n = SelectedItem.GetObjectVariable<LocalVariableString>(InitialNameKey);
        if (n.HasValue) n.Delete();
        LocalVariableString d = SelectedItem.GetObjectVariable<LocalVariableString>(InitialDescKey);
        if (d.HasValue) d.Delete();
    }

    public void Update(ItemDataRecord data)
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
            // Capture initial values immediately on selection
            EnsureInitialNameCaptured();
            EnsureInitialDescCaptured();
            OnNewSelection?.Invoke();
        }
        else
        {
            _player.SendServerMessage("Please target a valid item.", ColorConstants.Orange);
        }
    }

    // === Icon / Simple Model support (uses ItemModelValidation) ===
    public bool IsIconAllowed(out int current, out int max)
    {
        current = 0; max = 0;
        if (SelectedItem is null) return false;

        if (!ItemModelValidation.SupportsModelChanges(SelectedItem))
            return false;

        max = ItemModelValidation.GetMaxModelIndex(SelectedItem);
        if (max == 0) return false;

        current = NWScript.GetItemAppearance(SelectedItem, (int)ItemAppearanceType.SimpleModel, 0);
        return true;
    }

    public IconAdjustResult TryAdjustIcon(int delta, out int newValue, out int maxValue)
    {
        newValue = 0; maxValue = 0;
        if (SelectedItem is null) return IconAdjustResult.NoSelection;

        // Check if this item type supports model changes
        if (!ItemModelValidation.SupportsModelChanges(SelectedItem))
            return IconAdjustResult.NotAllowedType;

        maxValue = ItemModelValidation.GetMaxModelIndex(SelectedItem);
        if (maxValue == 0)
            return IconAdjustResult.NotAllowedType;

        int current = NWScript.GetItemAppearance(SelectedItem, (int)ItemAppearanceType.SimpleModel, 0);
        if (delta == 0)
        {
            newValue = current;
            return IconAdjustResult.Success;
        }

        // Get all valid indices for this item type
        List<int> validIndices = ItemModelValidation.GetValidIndices(SelectedItem).ToList();
        if (validIndices.Count == 0)
            return IconAdjustResult.NotAllowedType;

        // Find current index in the valid list (or closest)
        int currentIndex = validIndices.IndexOf(current);
        if (currentIndex == -1)
        {
            // Current model isn't in valid list; find closest
            currentIndex = validIndices.FindIndex(x => x >= current);
            if (currentIndex == -1) currentIndex = validIndices.Count - 1;
        }

        // Calculate new index with wrapping
        int newIndex = currentIndex + delta;
        while (newIndex < 0) newIndex += validIndices.Count;
        while (newIndex >= validIndices.Count) newIndex -= validIndices.Count;

        int candidate = validIndices[newIndex];

        // Verify the candidate is valid according to our dictionary
        if (!ItemModelValidation.IsValidModelIndex(SelectedItem, candidate))
            return IconAdjustResult.NoValidModel;

        // Apply the model change
        uint copy = NWScript.CopyItemAndModify(SelectedItem, (int)ItemAppearanceType.SimpleModel, 0, candidate, 1);
        if (NWScript.GetIsObjectValid(copy) == 1)
        {
            NWScript.DestroyObject(SelectedItem);
            SelectedItem = copy.ToNwObject<NwItem>();
            newValue = candidate;
            return IconAdjustResult.Success;
        }

        // Copy failed for some reason
        newValue = current;
        return IconAdjustResult.NoValidModel;
    }
}
