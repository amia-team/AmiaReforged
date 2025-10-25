using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.ItemEditor;

public sealed class ItemEditorPresenter : ScryPresenter<ItemEditorView>
{
    public override ItemEditorView View { get; }

    private readonly NwPlayer _player;
    private readonly ItemEditorModel _model;

    private NuiWindowToken _token;
    private NuiWindow? _window;

    // Local working copy of variables (we keep this in the presenter to render the manual column)
    private readonly List<(string Key, LocalVariableData Data)> _vars = new();

    // Modal references (optional – only needed if you want to programmatically close them)
    private NuiWindow? _editNameModal;
    private NuiWindow? _editDescModal;
    private NuiWindowToken _editNameToken;
    private NuiWindowToken _editDescToken;
    private bool _editNameOpen;
    private bool _editDescOpen;
    private NuiWindow? _editTagModal;
    private NuiWindowToken _editTagToken;

    private bool _initializing;

    public override NuiWindowToken Token() => _token;

    public ItemEditorPresenter(ItemEditorView view, NwPlayer player)
    {
        View = view;
        _player = player;
        _model = new ItemEditorModel(player);

        // When the model reports a new selection (after targeting), refresh UI
        _model.OnNewSelection += OnNewSelection;
    }
    public override void Create()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(400f, 120f, 800f, 720f),
            Resizable = true,
        };

        if (!_player.TryCreateNuiWindow(_window, out _token))
            return;

        _token.OnNuiEvent += ProcessEvent;

        // Initial bind state and UI setup
        _initializing = true;
        try
        {
            Token().SetBindValue(View.ValidObjectSelected, _model.SelectedItem != null);

            // We don't watch text fields for live-saves anymore
            Token().SetBindWatch(View.Name, false);
            Token().SetBindWatch(View.Description, false);
            Token().SetBindWatch(View.Tag, false);

            UpdateFromModel();
            RefreshIconInfo();
        }
        finally { _initializing = false; }
    }


    // ------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------
    public override void InitBefore()
    {

    }

    public override void Close()
    {
        Token().Close();
    }

    // ------------------------------------------------------------
    // Events
    // ------------------------------------------------------------
    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click && ev.EventType != NuiEventType.Open)
            return;

        // SELECT (target an item)
        if (ev.ElementId == View.SelectItemButton.Id)
        {
            _model.EnterTargetingMode();
            return;
        }

        // SAVE
        if (ev.ElementId == View.SaveButton.Id)
        {
            ApplyChanges(showMessage: true);
            return;
        }

        // ADD VARIABLE
        if (ev.ElementId == View.AddVariableButton.Id)
        {
            AddNewVariable();
            return;
        }

        // DELETE VARIABLE by encoded index: btn_del_var_{i}
        if (ev.ElementId.StartsWith("btn_del_var_", StringComparison.Ordinal))
        {
            if (int.TryParse(ev.ElementId.AsSpan("btn_del_var_".Length), out int idx))
            {
                DeleteVariableAt(idx);
                RebuildLayout(); // refresh the manual column
            }
            return;
        }

        // ICON bumps
        if (ev.ElementId == View.IconPlus1.Id)   { TryAdjustIcon(+1); return; }
        if (ev.ElementId == View.IconMinus1.Id)  { TryAdjustIcon(-1); return; }
        if (ev.ElementId == View.IconPlus10.Id)  { TryAdjustIcon(+10); return; }
        if (ev.ElementId == View.IconMinus10.Id) { TryAdjustIcon(-10); return; }

        // Open edit modals
        if (ev.ElementId == "btn_edit_name")
        {
            var snap = _model.SelectedItem != null ? ItemDataFactory.From(_model.SelectedItem) : null;
            Token().SetBindValue(View.EditNameBuffer, snap?.Name ?? "");
            _editNameModal = View.BuildEditNameModal();
            _player.TryCreateNuiWindow(_editNameModal, out _editNameToken);
            return;
        }

        if (ev.ElementId == "btn_edit_desc")
        {
            var snap = _model.SelectedItem != null ? ItemDataFactory.From(_model.SelectedItem) : null;
            Token().SetBindValue(View.EditDescBuffer, snap?.Description ?? "");
            _editDescModal = View.BuildEditDescModal();
            _player.TryCreateNuiWindow(_editDescModal, out _editDescToken);
            return;
        }

        // Modal OK / Cancel
        if (ev.ElementId == "btn_modal_ok_name")
        {
            var newName = Token().GetBindValue(View.EditNameBuffer) ?? string.Empty;
            Token().SetBindValue(View.Name, newName);
            ApplyChanges(showMessage: true);
            if (_editNameOpen) { _editNameToken.Close(); _editNameOpen = false; }
            return;
        }

        if (ev.ElementId == "btn_modal_cancel_name")
        {
            if (_editNameOpen) { _editNameToken.Close(); _editNameOpen = false; }
            return;
        }

        if (ev.ElementId == "btn_modal_ok_desc")
        {
            var newDesc = Token().GetBindValue(View.EditDescBuffer) ?? string.Empty;
            Token().SetBindValue(View.Description, newDesc);
            ApplyChanges(showMessage: true);
            if (_editDescOpen) { _editDescToken.Close(); _editDescOpen = false; }
            return;
        }

        if (ev.ElementId == "btn_modal_cancel_desc")
        {
            if (_editDescOpen) { _editDescToken.Close(); _editDescOpen = false; }
        }
        if (ev.ElementId == "btn_edit_tag")
        {
            var snap = _model.SelectedItem != null ? ItemDataFactory.From(_model.SelectedItem) : null;
            Token().SetBindValue(View.EditTagBuffer, snap?.Tag ?? "");
            _editTagModal = View.BuildEditTagModal();
            _player.TryCreateNuiWindow(_editTagModal, out _editTagToken);
            return;
        }
        if (ev.ElementId == "btn_modal_ok_tag")
        {
            var newTag = Token().GetBindValue(View.EditTagBuffer);
            Token().SetBindValue(View.Tag!, newTag);
            ApplyChanges(showMessage: true);
            _editTagToken.Close();
            return;
        }
        if (ev.ElementId == "btn_modal_cancel_tag")
        {
            _editTagToken.Close();
        }
    }

    private void OnNewSelection()
    {
        UpdateFromModel();
        RefreshIconInfo();
    }

    // ------------------------------------------------------------
    // Model/UI sync
    // ------------------------------------------------------------
    private void UpdateFromModel()
    {
        var item = _model.SelectedItem;
        Token().SetBindValue(View.ValidObjectSelected, item != null);
        // Always show the same placeholder for Description in the main window
        Token().SetBindValue(View.DescPlaceholder, item != null ? "Edit to View" : "");

        // Reset local vars list
        _vars.Clear();

        if (item == null)
        {
            Token().SetBindValue(View.Name, "");
            Token().SetBindValue(View.Description, "");
            Token().SetBindValue(View.Tag, "");
            RebuildLayout();
            return;
        }

        // Snapshot from the live item
        var snapshot = ItemDataFactory.From(item);

        Token().SetBindValue(View.Name, snapshot.Name);
        Token().SetBindValue(View.Description, snapshot.Description);
        Token().SetBindValue(View.Tag, snapshot.Tag);

        foreach (var kv in snapshot.Variables)
            _vars.Add((kv.Key, kv.Value));

        // Keep a stable, user-friendly order
        _vars.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));

        RebuildLayout();
        UpdateVariableList();
    }
    // Sync _vars -> view binds so any NuiList/Repeater bound to these updates in-place.
    private void UpdateVariableList()
    {
        // old parallel-bind updates can stay (harmless) — or remove if you’re fully on the fixed-rows UI
        var names  = new List<string>(_vars.Count);
        var types  = new List<string>(_vars.Count);
        var values = new List<string>(_vars.Count);

        foreach (var (key, data) in _vars)
        {
            names.Add(key);
            types.Add(data.Type.ToString());
            values.Add(ToDisplay(data));
        }

        Token().SetBindValue(View.VariableCount, _vars.Count);
        Token().SetBindValues(View.VariableNames,  names);
        Token().SetBindValues(View.VariableTypes,  types);
        Token().SetBindValues(View.VariableValues, values);

        // fixed-row pool updates (used by the transparent UI)
        int count = _vars.Count;
        int pool  = View.VarVisible.Length;

        for (int i = 0; i < pool; i++)
        {
            bool show = i < count;
            Token().SetBindValue(View.VarVisible[i], show);

            if (show)
            {
                var (key, data) = _vars[i];
                Token().SetBindValue(View.VarKey[i],   key);
                Token().SetBindValue(View.VarType[i],  data.Type.ToString());
                Token().SetBindValue(View.VarValue[i], ToDisplay(data));
            }
            else
            {
                // optional: clear hidden rows to avoid any ghost text when toggling
                Token().SetBindValue(View.VarKey[i],   "");
                Token().SetBindValue(View.VarType[i],  "");
                Token().SetBindValue(View.VarValue[i], "");
            }
        }
    }


    private void RebuildLayout()
    {
        // The variables section in the view rebuilds itself from binds? We’re using our _vars list.
        // So we need to push the display rows by calling the view’s builder with our current data.
        var rows = _vars.Select(v => (Name: v.Key,
                                      Type: v.Data.Type.ToString(),
                                      Value: ToDisplay(v.Data)))
                        .ToList();

        // We can’t “inject” the built group post-hoc without a subtree API,
        // but since RootLayout has just been set, the next paint uses current binds and our click IDs.
        // If you want true partial replacement, we can assign an ID to the variables group and swap that node.
        // For now the simple full-layout refresh keeps everything in sync.
        // (Nothing else to do here; RootLayout contains an empty variables section at build time.)
        // If you prefer, you can store rows somewhere the view reads; here we rely on IDs & click handlers.
        // No-op on purpose.
        _ = rows;
    }

    private static string ToDisplay(LocalVariableData data)
    {
        return data.Type switch
        {
            LocalVariableType.Int      => data.IntValue.ToString(),
            LocalVariableType.Float    => data.FloatValue.ToString("0.###"),
            LocalVariableType.String   => data.StringValue,
            LocalVariableType.Location => data.LocationValue?.ToString() ?? "(location)",
            LocalVariableType.Object   => data.ObjectValue?.ToString() ?? "(object)",
            _                          => ""
        };
    }

    // ------------------------------------------------------------
    // Apply / Variables manipulation
    // ------------------------------------------------------------
    private void ApplyChanges(bool showMessage)
    {
        if (_initializing) return;

        var item = _model.SelectedItem;
        if (item == null)
        {
            if (showMessage)
                _player.SendServerMessage("No item selected.", ColorConstants.Orange);
            return;
        }

        // Build ItemData payload for the model.Update(...)
        string name = Token().GetBindValue(View.Name) ?? string.Empty;
        string desc = Token().GetBindValue(View.Description) ?? string.Empty;
        string tag  = Token().GetBindValue(View.Tag) ?? string.Empty;

        var varsDict = new Dictionary<string, LocalVariableData>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, data) in _vars)
            varsDict[key] = data;

        var dataPacket = new ItemData(name, desc, tag, varsDict);

        _model.Update(dataPacket);

        if (showMessage)
            _player.SendServerMessage("Item updated successfully.", ColorConstants.Green);

        // Refresh from live item (in case the engine normalized anything)
        UpdateFromModel();
    }

    private void AddNewVariable()
    {
        if (_model.SelectedItem == null)
        {
            _player.SendServerMessage("Select an item first.", ColorConstants.Orange);
            return;
        }

        string name = Token().GetBindValue(View.NewVariableName) ?? string.Empty;
        string val  = Token().GetBindValue(View.NewVariableValue) ?? string.Empty;
        int    type = Token().GetBindValue(View.NewVariableType);

        if (string.IsNullOrWhiteSpace(name))
        {
            _player.SendServerMessage("Variable name cannot be empty.", ColorConstants.Orange);
            return;
        }

        // Convert the textual input to LocalVariableData
        var data = ParseLocalVarInput((LocalVariableType)type, val, out string? error);
        if (error != null)
        {
            _player.SendServerMessage(error, ColorConstants.Orange);
            return;
        }

        // Upsert into local list (case-insensitive on key)
        int existingIdx = _vars.FindIndex(v => v.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existingIdx >= 0) _vars[existingIdx] = (name, data);
        else _vars.Add((name, data));

        // Keep order consistent
        _vars.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));

        // Clear entry fields (optional)
        Token().SetBindValue(View.NewVariableName, "");
        Token().SetBindValue(View.NewVariableValue, "");

        RebuildLayout();
        UpdateVariableList();
    }

    private void DeleteVariableAt(int index)
    {
        if (index < 0 || index >= _vars.Count) return;
        _vars.RemoveAt(index);
        UpdateVariableList();
    }

    private static LocalVariableData ParseLocalVarInput(LocalVariableType type, string raw, out string? error)
    {
        error = null;
        switch (type)
        {
            case LocalVariableType.Int:
                if (!int.TryParse(raw, out int i)) { error = "Value must be an integer."; return default!; }
                return new LocalVariableData { Type = type, IntValue = i };

            case LocalVariableType.Float:
                if (!float.TryParse(raw, out float f)) { error = "Value must be a number."; return default!; }
                return new LocalVariableData { Type = type, FloatValue = f };

            case LocalVariableType.String:
                return new LocalVariableData { Type = type, StringValue = raw };

            case LocalVariableType.Location:
                return new LocalVariableData { Type = type, LocationValue = null };

            case LocalVariableType.Object:
                return new LocalVariableData { Type = type, ObjectValue = null };

            default:
                error = "Unsupported variable type.";
                return default!;
        }
    }

    // ------------------------------------------------------------
    // Icon helpers
    // ------------------------------------------------------------
    private void TryAdjustIcon(int delta)
    {
        var result = _model.TryAdjustIcon(delta, out int newValue, out int maxValue);
        switch (result)
        {
            case ItemEditorModel.IconAdjustResult.NoSelection:
                _player.SendServerMessage("Select an item first.", ColorConstants.Orange);
                break;
            case ItemEditorModel.IconAdjustResult.NotAllowedType:
                _player.SendServerMessage("This item type can't change icons in this tool.", ColorConstants.Orange);
                break;
            case ItemEditorModel.IconAdjustResult.Success:
                Token().SetBindValue(View.IconInfo, $"Icon: {newValue} / {maxValue}");
                _player.SendServerMessage("Icon updated.", ColorConstants.Green);
                break;
        }
    }

    private void RefreshIconInfo()
    {
        bool allowed = _model.IsIconAllowed(out int current, out int max);
        Token().SetBindValue(View.IconControlsVisible, allowed);
        Token().SetBindValue(View.IconInfo, allowed ? $"Icon: {current} / {max}" : "Icon: —");
    }
}
