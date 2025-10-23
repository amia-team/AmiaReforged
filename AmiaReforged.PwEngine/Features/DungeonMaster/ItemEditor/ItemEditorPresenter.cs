using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.ItemEditor;

public sealed class ItemEditorPresenter : ScryPresenter<ItemEditorView>
{
    public override ItemEditorView View { get; }

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private bool _isInitializing;

    public override NuiWindowToken Token() => _token;

    private readonly ItemEditorModel _model;
    private readonly Dictionary<string, LocalVariableData> _trackedVariables = new();

    public ItemEditorPresenter(ItemEditorView itemEditorView, NwPlayer player)
    {
        View = itemEditorView;
        _player = player;
        _model = new ItemEditorModel(player);
        _model.OnNewSelection += UpdateFromSelection;
    }

    private void UpdateFromSelection()
    {
        UpdateFromModel();
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(300f, 200f, 800f, 720f)
        };
    }

    public override void Create()
    {
        if (_window == null)
            InitBefore();

        if (_window == null)
        {
            _player.SendServerMessage(
                "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        Token().SetBindValue(View.ValidObjectSelected, _model.SelectedItem != null);

        // ❶ Populate the UI from the model first.
        UpdateFromModel();

        // ❷ Only then start watching for edits.
        Token().SetBindWatch(View.Name, true);
        Token().SetBindWatch(View.Description, true);
        Token().SetBindWatch(View.Tag, true);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(eventData);
                break;
            case NuiEventType.Watch:
                HandleWatchUpdate(eventData.ElementId);
                break;
        }
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == View.SelectItemButton.Id)
        {
            _model.EnterTargetingMode();
        }
        else if (eventData.ElementId == View.SaveButton.Id)
        {
            ApplyChanges(true);
        }
        else if (eventData.ElementId == View.AddVariableButton.Id)
        {
            AddNewVariable();
        }
        else if (eventData.ElementId == View.DeleteVariableButton.Id && eventData.ArrayIndex >= 0)
        {
            DeleteVariable(eventData.ArrayIndex);
        }
        else if (_model.SelectedItem != null)
        {
            // Icon clicks
            if (eventData.ElementId == View.IconPlus1.Id)       TryAdjustIcon(+1);
            else if (eventData.ElementId == View.IconMinus1.Id) TryAdjustIcon(-1);
            else if (eventData.ElementId == View.IconPlus10.Id) TryAdjustIcon(+10);
            else if (eventData.ElementId == View.IconMinus10.Id)TryAdjustIcon(-10);
        }
    }

    private void HandleWatchUpdate(string? elementId)
    {
        if (_model.SelectedItem == null)
        {
            UpdateFromModel();
            return;
        }

        if (_isInitializing) return; // prevent first populate from auto-saving

        // Auto-apply changes as they're made
        ApplyChanges(false);
    }

    private void ApplyChanges(bool showSuccessMessage)
    {
        if (_model.SelectedItem == null) return;

        string? newName = Token().GetBindValue(View.Name);
        string? newDescription = Token().GetBindValue(View.Description);
        string? newTag = Token().GetBindValue(View.Tag);
        if (newName is null || newDescription is null || newTag is null) return;

        ItemData newData = new(newName, newDescription, newTag, _trackedVariables);
        _model.Update(newData);

        if (showSuccessMessage)
            _player.SendServerMessage("Item updated successfully.", ColorConstants.Green);
    }

    private void TryAdjustIcon(int delta)
    {
        var result = _model.TryAdjustIcon(delta, out int newValue, out int maxValue);
        switch (result)
        {
            case ItemEditorModel.IconAdjustResult.NotAllowedType:
                _player.SendServerMessage("This item type can't change icons in this tool.", ColorConstants.Orange);
                break;
            case ItemEditorModel.IconAdjustResult.Success:
                Token().SetBindValue(View.IconInfo, $"Icon: {newValue} / {maxValue}");
                _player.SendServerMessage("Icon updated.", ColorConstants.Green);
                break;
            case ItemEditorModel.IconAdjustResult.NoSelection:
                break;
        }
    }

    private void AddNewVariable()
    {
        string? varName = Token().GetBindValue(View.NewVariableName);
        int varTypeIndex = Token().GetBindValue(View.NewVariableType);
        string? varValue = Token().GetBindValue(View.NewVariableValue);

        if (string.IsNullOrWhiteSpace(varName))
        {
            _player.SendServerMessage("Variable name cannot be empty.", ColorConstants.Red);
            return;
        }

        LocalVariableType type = (LocalVariableType)varTypeIndex;
        LocalVariableData data = CreateVariableData(type, varValue ?? string.Empty);

        bool existed = _trackedVariables.ContainsKey(varName);
        _trackedVariables[varName] = data; // UPSERT

        UpdateVariableList();
        ApplyChanges(true);

        _player.SendServerMessage(
            existed ? $"Variable '{varName}' updated." : $"Variable '{varName}' added.",
            ColorConstants.Green);

        // Clear inputs
        Token().SetBindValue(View.NewVariableName, string.Empty);
        Token().SetBindValue(View.NewVariableValue, string.Empty);
    }

    private LocalVariableData CreateVariableData(LocalVariableType type, string value)
    {
        return type switch
        {
            LocalVariableType.Int => new LocalVariableData
            {
                Type = LocalVariableType.Int,
                IntValue = int.TryParse(value, out int i) ? i : 0
            },
            LocalVariableType.Float => new LocalVariableData
            {
                Type = LocalVariableType.Float,
                FloatValue = float.TryParse(value, out float f) ? f : 0f
            },
            LocalVariableType.String => new LocalVariableData
            {
                Type = LocalVariableType.String,
                StringValue = value
            },
            _ => new LocalVariableData { Type = LocalVariableType.String, StringValue = value }
        };
    }

    private void DeleteVariable(int index)
    {
        List<string> keys = _trackedVariables.Keys.ToList();
        if (index < 0 || index >= keys.Count) return;

        string keyToRemove = keys[index];
        _trackedVariables.Remove(keyToRemove);
        UpdateVariableList();
        ApplyChanges(true);
    }

    private void UpdateVariableList()
    {
        List<string> varNames = _trackedVariables.Keys.ToList();
        List<string> varValues = _trackedVariables.Values
            .Select(v => v.Type switch
            {
                LocalVariableType.Int => v.IntValue.ToString(),
                LocalVariableType.Float => v.FloatValue.ToString("F2"),
                LocalVariableType.String => v.StringValue,
                _ => string.Empty
            })
            .ToList();
        List<string> varTypes = _trackedVariables.Values
            .Select(v => v.Type.ToString())
            .ToList();

        Token().SetBindValue(View.VariableCount, _trackedVariables.Count);
        Token().SetBindValues(View.VariableNames, varNames);
        Token().SetBindValues(View.VariableValues, varValues);
        Token().SetBindValues(View.VariableTypes, varTypes);
    }

    private void UpdateFromModel()
    {
        _isInitializing = true;
        try
        {
            bool selectionAvailable = _model.SelectedItem != null;
            Token().SetBindValue(View.ValidObjectSelected, selectionAvailable);

            if (_model.SelectedItem is null) return;

            // Basic fields
            Token().SetBindValue(View.Name, _model.SelectedItem.Name);
            Token().SetBindValue(View.Description, _model.SelectedItem.Description);
            Token().SetBindValue(View.Tag, _model.SelectedItem.Tag);

            // Seed the tracked variables from the item BEFORE any watch can apply changes
            ItemData current = ItemDataFactory.From(_model.SelectedItem);
            _trackedVariables.Clear();
            foreach (KeyValuePair<string, LocalVariableData> kvp in current.Variables)
                _trackedVariables[kvp.Key] = kvp.Value;

            UpdateVariableList();

            // Icon controls
            bool iconAllowed = _model.IsIconAllowed(out int currentIcon, out int maxIcon);
            Token().SetBindValue(View.IconControlsVisible, iconAllowed);
            Token().SetBindValue(View.IconInfo, iconAllowed ? $"Icon: {currentIcon} / {maxIcon}" : "Icon: —");
        }
        finally
        {
            _isInitializing = false;
        }
    }

    public override void Close()
    {
        Token().Close();
    }
}
