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
    private string _variableFilterTerm = string.Empty; // Current filter term for variable search

    // Modal window tokens
    private NuiWindowToken? _nameModalToken;
    private NuiWindowToken? _descModalToken;
    private NuiWindowToken? _tagModalToken;
    private NuiWindowToken? _editVariableModalToken;

    private bool _initializing;
    private bool _addingVariable;
    private bool _applyingChanges;
    private bool _processingEvent;
    private int _editingVariableIndex = -1; // Track which variable is being edited

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

        // Initial bind state and UI setup
        _initializing = true;
        try
        {
            Token().SetBindValue(View.ValidObjectSelected, _model.SelectedItem != null);

            // We don't watch text fields for live-saves anymore
            Token().SetBindWatch(View.Name, false);
            Token().SetBindWatch(View.Description, false);
            Token().SetBindWatch(View.Tag, false);

            // Initialize variable type combo with a default value to prevent null bind errors
            View.InitializeVariableTypeDefault(Token());

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
        // Global re-entry guard for all events
        if (_processingEvent)
            return;
        _processingEvent = true;

        try
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

            if (ev.ElementId == View.CancelButton.Id)
            {
                Close();
                return;
            }

            // ADD VARIABLE
            if (ev.ElementId == View.AddVariableButton.Id)
            {
                AddNewVariable();
                return;
            }

            // SEARCH VARIABLES - filter by name
            if (ev.ElementId == "btn_search_var")
            {
                SearchVariables();
                return;
            }

            // DELETE VARIABLE - NuiList provides the index automatically
            if (ev.ElementId == "btn_del_var")
            {
                DeleteVariableAt(ev.ArrayIndex);
                return;
            }

            // EDIT VARIABLE - NuiList provides the index automatically
            if (ev.ElementId == "btn_edit_var")
            {
                EditVariableAt(ev.ArrayIndex);
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
                // Prevent opening if modal already exists
                if (_nameModalToken.HasValue)
                    return;

                // Capture initial name and set buffer BEFORE creating modal
                _model.EnsureInitialNameCaptured();
                Token().SetBindValue(View.EditNameBuffer, _model.GetInitialNameOrCurrent());

                NuiWindow w = View.BuildEditNameModal();
                if (_player.TryCreateNuiWindow(w, out NuiWindowToken modalToken))
                {
                    _nameModalToken = modalToken;
                    modalToken.SetBindValue(View.EditNameBuffer, _model.SelectedItem!.Name);
                    _nameModalToken.Value.OnNuiEvent += HandleNameModalEvent;
                }
                return;
            }

            if (ev.ElementId == "btn_edit_desc")
            {
                // Prevent opening if modal already exists
                if (_descModalToken.HasValue)
                    return;

                // Capture initial desc and set buffer BEFORE creating modal
                _model.EnsureInitialDescCaptured();
                Token().SetBindValue(View.EditDescBuffer, _model.GetInitialDescOrCurrent());

                NuiWindow w = View.BuildEditDescModal();
                if (_player.TryCreateNuiWindow(w, out NuiWindowToken modalToken))
                {
                    _descModalToken = modalToken;
                    modalToken.SetBindValue(View.EditDescBuffer, _model.SelectedItem!.Description);
                    _descModalToken.Value.OnNuiEvent += HandleDescModalEvent;
                }
                return;
            }

            if (ev.ElementId == "btn_edit_tag")
            {
                // Prevent opening if modal already exists
                if (_tagModalToken.HasValue)
                    return;

                ItemDataRecord? snap = _model.SelectedItem != null ? ItemDataFactory.From(_model.SelectedItem) : null;
                Token().SetBindValue(View.EditTagBuffer, snap?.Tag ?? "");
                NuiWindow w = View.BuildEditTagModal();
                if (_player.TryCreateNuiWindow(w, out NuiWindowToken modalToken))
                {
                    _tagModalToken = modalToken;
                    modalToken.SetBindValue(View.EditTagBuffer, snap?.Tag ?? "");
                    _tagModalToken.Value.OnNuiEvent += HandleTagModalEvent;
                }
            }

            // EDIT VARIABLE - modal opens from button click in variable list
            if (ev.ElementId == "btn_modal_ok_var")
            {
                HandleEditVariableModalEvent(ev);
                return;
            }

            if (ev.ElementId == "btn_modal_cancel_var")
            {
                HandleEditVariableModalEvent(ev);
                return;
            }
        }
        finally
        {
            _processingEvent = false;
        }
    }

    private void OnNewSelection()
    {
        UpdateFromModel();
        RefreshIconInfo();
    }

    // ------------------------------------------------------------
    // Modal Event Handlers
    // ------------------------------------------------------------
    private void HandleNameModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click)
            return;

        if (ev.ElementId == "btn_modal_ok_name")
        {
            string newName = _nameModalToken!.Value.GetBindValue(View.EditNameBuffer) ?? string.Empty;
            _model.SelectedItem!.Name = newName;
            Token().SetBindValue(View.Name, newName);
            _player.SendServerMessage("Name saved.", ColorConstants.Green);
            if (_nameModalToken.HasValue)
            {
                _nameModalToken.Value.OnNuiEvent -= HandleNameModalEvent;
                _nameModalToken?.Close();
                _nameModalToken = null;
            }
            return;
        }

        if (ev.ElementId == "btn_modal_cancel_name")
        {
            _model.RevertNameToInitial();
            Token().SetBindValue(View.Name, _model.GetInitialNameOrCurrent());
            if (_nameModalToken.HasValue)
            {
                _nameModalToken.Value.OnNuiEvent -= HandleNameModalEvent;
                _nameModalToken?.Close();
                _nameModalToken = null;
            }
        }
    }

    private void HandleDescModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click)
            return;

        if (ev.ElementId == "btn_modal_ok_desc")
        {
            string newDesc = _descModalToken!.Value.GetBindValue(View.EditDescBuffer) ?? string.Empty;
            _model.SelectedItem!.Description = newDesc;
            Token().SetBindValue(View.Description, newDesc);
            _player.SendServerMessage("Description saved.", ColorConstants.Green);
            if (_descModalToken.HasValue)
            {
                _descModalToken.Value.OnNuiEvent -= HandleDescModalEvent;
                _descModalToken?.Close();
                _descModalToken = null;
            }
            return;
        }

        if (ev.ElementId == "btn_modal_cancel_desc")
        {
            _model.RevertDescToInitial();
            Token().SetBindValue(View.Description, _model.GetInitialDescOrCurrent());
            if (_descModalToken.HasValue)
            {
                _descModalToken.Value.OnNuiEvent -= HandleDescModalEvent;
                _descModalToken?.Close();
                _descModalToken = null;
            }
        }
    }

    private void HandleTagModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click)
            return;

        if (ev.ElementId == "btn_modal_ok_tag")
        {
            string newTag = _tagModalToken!.Value.GetBindValue(View.EditTagBuffer) ?? string.Empty;
            _model.SelectedItem!.Tag = newTag;
            Token().SetBindValue(View.Tag, newTag);
            _player.SendServerMessage("Tag saved.", ColorConstants.Green);
            if (_tagModalToken.HasValue)
            {
                _tagModalToken.Value.OnNuiEvent -= HandleTagModalEvent;
                _tagModalToken?.Close();
                _tagModalToken = null;
            }
            return;
        }

        if (ev.ElementId == "btn_modal_cancel_tag")
        {
            if (_tagModalToken.HasValue)
            {
                _tagModalToken.Value.OnNuiEvent -= HandleTagModalEvent;
                _tagModalToken?.Close();
                _tagModalToken = null;
            }
        }
    }

    private void HandleEditVariableModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click)
            return;

        if (ev.ElementId == "btn_modal_ok_var")
        {
            // Validate the editing index is still valid
            if (_editingVariableIndex < 0 || _editingVariableIndex >= _vars.Count)
            {
                _player.SendServerMessage("Variable index out of range.", ColorConstants.Orange);
                CloseEditVariableModal();
                return;
            }

            // Get the new values from the modal
            string newValue = _editVariableModalToken!.Value.GetBindValue(View.EditVariableValue) ?? string.Empty;
            int newType = 0;
            try
            {
                newType = _editVariableModalToken.Value.GetBindValue(View.EditVariableType);
            }
            catch
            {
                newType = 0; // Default to Int if reading fails
            }

            // Parse the value with the new type
            LocalVariableData newData = ParseLocalVarInput((LocalVariableType)newType, newValue, out string? error);
            if (error != null)
            {
                _player.SendServerMessage(error, ColorConstants.Orange);
                return;
            }

            // Update the variable in our list (keep the same key)
            var (key, _) = _vars[_editingVariableIndex];
            _vars[_editingVariableIndex] = (key, newData);

            _player.SendServerMessage($"Variable '{key}' updated.", ColorConstants.Green);

            CloseEditVariableModal();
            UpdateVariableList();
            return;
        }

        if (ev.ElementId == "btn_modal_cancel_var")
        {
            CloseEditVariableModal();
        }
    }

    private void CloseEditVariableModal()
    {
        if (_editVariableModalToken.HasValue)
        {
            _editVariableModalToken.Value.OnNuiEvent -= HandleEditVariableModalEvent;
            _editVariableModalToken?.Close();
            _editVariableModalToken = null;
        }
        _editingVariableIndex = -1;
    }

    // ------------------------------------------------------------
    // Model/UI sync
    // ------------------------------------------------------------
    private void UpdateFromModel()
    {
        NwItem? item = _model.SelectedItem;
        Token().SetBindValue(View.ValidObjectSelected, item != null);
        // Always show the same placeholder for Description in the main window
        Token().SetBindValue(View.DescPlaceholder, item != null ? "Edit to View" : "");

        // Reset local vars list and filter
        _vars.Clear();
        _variableFilterTerm = string.Empty; // Clear filter when changing items

        if (item == null)
        {
            Token().SetBindValue(View.Name, "");
            Token().SetBindValue(View.Description, "");
            Token().SetBindValue(View.Tag, "");
            RebuildLayout();
            return;
        }

        // Snapshot from the live item
        ItemDataRecord snapshot = ItemDataFactory.From(item);

        Token().SetBindValue(View.Name, snapshot.Name);
        Token().SetBindValue(View.Description, snapshot.Description);
        Token().SetBindValue(View.Tag, snapshot.Tag);

        foreach (KeyValuePair<string, LocalVariableData> kv in snapshot.Variables)
            _vars.Add((kv.Key, kv.Value));

        // Keep a stable, user-friendly order
        _vars.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));

        RebuildLayout();
        UpdateVariableList();
    }
    // Sync _vars -> view binds for the NuiList
    private void UpdateVariableList()
    {
        List<string> names  = new List<string>();
        List<string> types  = new List<string>();
        List<string> values = new List<string>();

        // Filter variables based on current search term
        var filteredVars = string.IsNullOrEmpty(_variableFilterTerm)
            ? _vars
            : _vars.Where(v => v.Key.Contains(_variableFilterTerm, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach ((string key, LocalVariableData data) in filteredVars)
        {
            names.Add(key);
            types.Add(data.Type.ToString());
            values.Add(ToDisplay(data));
        }

        Token().SetBindValue(View.VariableCount, filteredVars.Count);
        Token().SetBindValues(View.VariableNames,  names);
        Token().SetBindValues(View.VariableTypes,  types);
        Token().SetBindValues(View.VariableValues, values);
    }


    private void RebuildLayout()
    {
        // The variables section in the view rebuilds itself from binds? We’re using our _vars list.
        // So we need to push the display rows by calling the view’s builder with our current data.
        List<(string Name, string Type, string Value)> rows = _vars.Select(v => (Name: v.Key,
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

        // Prevent re-entry
        if (_applyingChanges)
            return;
        _applyingChanges = true;

        try
        {

            NwItem? item = _model.SelectedItem;
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

            Dictionary<string, LocalVariableData> varsDict = new Dictionary<string, LocalVariableData>(StringComparer.OrdinalIgnoreCase);
            foreach ((string key, LocalVariableData data) in _vars)
                varsDict[key] = data;

            ItemDataRecord dataPacket = new ItemDataRecord(name, desc, tag, varsDict);

            _model.Update(dataPacket);

            // Clear initial values after successful save
            _model.ClearInitials();

            if (showMessage)
                _player.SendServerMessage("Item updated successfully.", ColorConstants.Green);

            // Refresh from live item (in case the engine normalized anything)
            UpdateFromModel();
        }
        finally
        {
            _applyingChanges = false;
        }
    }

    private void AddNewVariable()
    {
        // Prevent re-entry (button click might trigger multiple times)
        if (_addingVariable)
            return;
        _addingVariable = true;

        try
        {

            if (_model.SelectedItem == null)
            {
                _player.SendServerMessage("Select an item first.", ColorConstants.Orange);
                return;
            }

            string name = Token().GetBindValue(View.NewVariableName) ?? string.Empty;
            string val  = Token().GetBindValue(View.NewVariableValue) ?? string.Empty;

            // Read the variable type - will be 0 (Int) by default from initialization
            int type = 0;
            try
            {
                type = Token().GetBindValue(View.NewVariableType);
            }
            catch
            {
                // If reading fails, default to Int (0)
                type = 0;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                _player.SendServerMessage("Variable name cannot be empty.", ColorConstants.Orange);
                return;
            }

            // Convert the textual input to LocalVariableData
            LocalVariableData data = ParseLocalVarInput((LocalVariableType)type, val, out string? error);
            if (error != null)
            {
                _player.SendServerMessage(error, ColorConstants.Orange);
                return;
            }

            // Upsert into local list (case-insensitive on key)
            int existingIdx = _vars.FindIndex(v => v.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existingIdx >= 0)
            {
                _vars[existingIdx] = (name, data);
                _player.SendServerMessage($"Variable '{name}' updated.", ColorConstants.Green);
            }
            else
            {
                _vars.Add((name, data));
                _player.SendServerMessage($"Variable '{name}' added.", ColorConstants.Green);
            }

            // Keep order consistent
            _vars.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));

            // Clear entry fields
            Token().SetBindValue(View.NewVariableName, "");
            Token().SetBindValue(View.NewVariableValue, "");

            RebuildLayout();
            UpdateVariableList();
        }
        finally
        {
            _addingVariable = false;
        }
    }

    private void DeleteVariableAt(int index)
    {
        if (index < 0 || index >= _vars.Count) return;

        // Remove from local list - the actual deletion will happen when ApplyChanges is called
        string keyToDelete = _vars[index].Key;
        _vars.RemoveAt(index);

        // Inform user of deletion
        _player.SendServerMessage($"Variable '{keyToDelete}' marked for deletion. Click Save to confirm.", ColorConstants.Green);

        UpdateVariableList();
    }

    private void EditVariableAt(int index)
    {
        if (index < 0 || index >= _vars.Count) return;

        // Prevent opening if modal already exists
        if (_editVariableModalToken.HasValue)
            return;

        // Get the variable to edit
        var (key, data) = _vars[index];
        _editingVariableIndex = index;

        // Open the edit modal
        NuiWindow w = View.BuildEditVariableModal();
        if (_player.TryCreateNuiWindow(w, out NuiWindowToken modalToken))
        {
            _editVariableModalToken = modalToken;

            // Initialize the type combo with a default value to prevent null bind errors
            View.InitializeEditVariableTypeDefault(_editVariableModalToken.Value);

            // Set the binds in the modal
            modalToken.SetBindValue(View.EditVariableName, key);
            modalToken.SetBindValue(View.EditVariableType, (int)data.Type);
            modalToken.SetBindValue(View.EditVariableValue, ToDisplay(data));

            _editVariableModalToken.Value.OnNuiEvent += HandleEditVariableModalEvent;
        }
    }

    private void SearchVariables()
    {
        // Get the search term from the Variable Name input field
        string searchTerm = Token().GetBindValue(View.NewVariableName) ?? string.Empty;

        // If search term is empty, show all variables
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            _variableFilterTerm = string.Empty;
            _player.SendServerMessage("Filter cleared. Showing all variables.", ColorConstants.Green);
        }
        else
        {
            _variableFilterTerm = searchTerm.Trim();
            int matchCount = _vars.Count(v => v.Key.Contains(_variableFilterTerm, StringComparison.OrdinalIgnoreCase));
            _player.SendServerMessage($"Filter applied: '{_variableFilterTerm}' - {matchCount} matching variable(s).", ColorConstants.Green);
        }

        // Refresh the display with the filter applied
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
        IconAdjustResult result = _model.TryAdjustIcon(delta, out int newValue, out int maxValue);
        switch (result)
        {
            case IconAdjustResult.NoSelection:
                _player.SendServerMessage("Select an item first.", ColorConstants.Orange);
                break;
            case IconAdjustResult.NotAllowedType:
                _player.SendServerMessage("This item type can't change icons in this tool.", ColorConstants.Orange);
                break;
            case IconAdjustResult.NoValidModel:
                _player.SendServerMessage("No valid model found for this item.", ColorConstants.Orange);
                break;
            case IconAdjustResult.Success:
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
