using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

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

        foreach (var local in SelectedItem.LocalVariables)
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
}
