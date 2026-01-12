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
    private NuiWindowToken? _editItemTypeModalToken;
    private NuiWindowToken? _confirmNoDamageModalToken;

    private bool _initializing;
    private bool _addingVariable;
    private bool _applyingChanges;
    private bool _processingEvent;
    private int _editingVariableIndex = -1; // Track which variable is being edited

    // Item type change tracking
    private List<(BaseItemType Type, string ResRef)> _compatibleItemTypes = new();
    private int _selectedItemTypeIndex = -1;

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
            Closable = false,
            Resizable = true
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

            if (ev.ElementId == "btn_edit_itemtype")
            {
                EditItemType();
                return;
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

            // EDIT ITEM TYPE modal buttons
            if (ev.ElementId == "btn_modal_ok_itemtype")
            {
                HandleEditItemTypeModalEvent(ev);
                return;
            }

            if (ev.ElementId == "btn_modal_cancel_itemtype")
            {
                CloseEditItemTypeModal();
                return;
            }

            // CONFIRM NO DAMAGE modal buttons
            if (ev.ElementId == "btn_confirm_nodamage")
            {
                ProceedWithNoDamageChange();
                return;
            }

            if (ev.ElementId == "btn_cancel_nodamage")
            {
                CloseConfirmNoDamageModal();
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

        if (ev.ElementId == "btn_clear_desc")
        {
            _descModalToken!.Value.SetBindValue(View.EditDescBuffer, string.Empty);
            _player.SendServerMessage("Description cleared.", ColorConstants.Green);
            return;
        }

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

        if (ev.ElementId == "btn_clear_var")
        {
            _editVariableModalToken!.Value.SetBindValue(View.EditVariableValue, string.Empty);
            _player.SendServerMessage("Variable value cleared.", ColorConstants.Green);
            return;
        }

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
            Token().SetBindValue(View.ItemType, "");
            RebuildLayout();
            return;
        }

        // Snapshot from the live item
        ItemDataRecord snapshot = ItemDataFactory.From(item);

        Token().SetBindValue(View.Name, snapshot.Name);
        Token().SetBindValue(View.Description, snapshot.Description);
        Token().SetBindValue(View.Tag, snapshot.Tag);
        Token().SetBindValue(View.ItemType, GetItemTypeName(item.BaseItem.ItemType));

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
        Token().SetBindValue(View.IconInfo, allowed ? $"Icon: {current} / {max}" : "Icon: -");
    }

    // ============================================================
    // Item Type Change Methods
    // ============================================================

    private void EditItemType()
    {
        if (_model.SelectedItem == null)
        {
            _player.SendServerMessage("No item selected.", ColorConstants.Orange);
            return;
        }

        if (_editItemTypeModalToken.HasValue)
            return;

        // Get compatible item types for the current item's type
        _compatibleItemTypes = GetBaseItemTypeMapping(_model.SelectedItem.BaseItem.ItemType);

        if (_compatibleItemTypes.Count == 0)
        {
            _player.SendServerMessage("No compatible item types found for this item.", ColorConstants.Orange);
            return;
        }

        // Build combo entries for the modal
        List<NuiComboEntry> entries = new();

        // Check if this is armor - show AC levels instead of item types
        if (_model.SelectedItem.BaseItem.ItemType == BaseItemType.Armor)
        {
            for (int i = 0; i < _compatibleItemTypes.Count; i++)
            {
                entries.Add(new NuiComboEntry($"AC {i}", i));
            }
        }
        else
        {
            // For 1H weapons, add warning indicator for non-weapon items that can't take weapon properties
            bool is1HCategory = _model.SelectedItem.BaseItem.ItemType == BaseItemType.Shortsword ||
                               _model.SelectedItem.BaseItem.ItemType == BaseItemType.Longsword ||
                               _model.SelectedItem.BaseItem.ItemType == BaseItemType.Dagger ||
                               (int)_model.SelectedItem.BaseItem.ItemType >= 93; // Custom items

            List<int> limitedItemRows = new() { 93, 15, 94, 113, 222, 223 }; // Trumpet, Torch, Moon, Tools Left, Focus, Umbrella

            for (int i = 0; i < _compatibleItemTypes.Count; i++)
            {
                string displayName = GetItemTypeName(_compatibleItemTypes[i].Item1);

                // Add warning symbol for 1H non-weapon items that can't take weapon properties
                if (is1HCategory && limitedItemRows.Contains((int)_compatibleItemTypes[i].Item1))
                {
                    displayName = $"[NO DAMAGE] {displayName}";
                }

                entries.Add(new NuiComboEntry(displayName, i));
            }
        }

        // Open the modal
        NuiWindow w = View.BuildEditItemTypeModal(entries);
        if (_player.TryCreateNuiWindow(w, out NuiWindowToken modalToken))
        {
            _editItemTypeModalToken = modalToken;
            // Initialize with default selection
            modalToken.SetBindValue(View.EditItemTypeSelection, 0);
            _editItemTypeModalToken.Value.OnNuiEvent += HandleEditItemTypeModalEvent;
        }
    }

    private void HandleEditItemTypeModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click)
            return;

        if (ev.ElementId == "btn_modal_ok_itemtype")
        {
            ConfirmItemTypeChange();
            return;
        }

        if (ev.ElementId == "btn_modal_cancel_itemtype")
        {
            CloseEditItemTypeModal();
            return;
        }
    }

    private async void ConfirmItemTypeChange()
    {
        if (_model.SelectedItem == null || !_model.SelectedItem.IsValid)
        {
            _player.SendServerMessage("Item is no longer valid.", ColorConstants.Orange);
            CloseEditItemTypeModal();
            return;
        }

        int selection = 0;
        try
        {
            selection = _editItemTypeModalToken!.Value.GetBindValue(View.EditItemTypeSelection);
        }
        catch
        {
            _player.SendServerMessage("Invalid selection.", ColorConstants.Orange);
            CloseEditItemTypeModal();
            return;
        }

        if (selection < 0 || selection >= _compatibleItemTypes.Count)
        {
            _player.SendServerMessage("Invalid item type selected.", ColorConstants.Orange);
            CloseEditItemTypeModal();
            return;
        }

        var (newType, newResRef) = _compatibleItemTypes[selection];
        string currentName = _model.SelectedItem.Name;
        string currentDesc = _model.SelectedItem.Description;
        string currentTag = _model.SelectedItem.Tag;
        Location currentLocation = _model.SelectedItem.Location;

        // Check if trying to change to a [NO DAMAGE] item type with weapon properties
        List<int> noDamageItemRows = new() { 93, 15, 94, 113, 222, 223 }; // Trumpet, Torch, Moon, Tools Left, Focus, Umbrella

        if (noDamageItemRows.Contains((int)newType) && HasWeaponProperties(_model.SelectedItem))
        {
            // Show confirmation modal instead of blocking
            ShowConfirmNoDamageModal(selection);
            return;
        }

        // No issues, proceed with the item type change
        await PerformItemTypeChange(newType, newResRef, currentName, currentDesc, currentTag, currentLocation);
    }

    private void ShowConfirmNoDamageModal(int selectedIndex)
    {
        if (_confirmNoDamageModalToken.HasValue)
            return;

        _selectedItemTypeIndex = selectedIndex; // Store which item type was selected

        NuiWindow w = View.BuildConfirmNoDamageItemTypeModal();
        if (_player.TryCreateNuiWindow(w, out NuiWindowToken modalToken))
        {
            _confirmNoDamageModalToken = modalToken;
            _confirmNoDamageModalToken.Value.OnNuiEvent += HandleConfirmNoDamageModalEvent;
        }
    }

    private void HandleConfirmNoDamageModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click)
            return;

        if (ev.ElementId == "btn_confirm_nodamage")
        {
            ProceedWithNoDamageChange();
            return;
        }

        if (ev.ElementId == "btn_cancel_nodamage")
        {
            CloseConfirmNoDamageModal();
            return;
        }
    }

    private async void ProceedWithNoDamageChange()
    {
        if (_model.SelectedItem == null || !_model.SelectedItem.IsValid)
        {
            _player.SendServerMessage("Item is no longer valid.", ColorConstants.Orange);
            CloseConfirmNoDamageModal();
            return;
        }

        if (_selectedItemTypeIndex < 0 || _selectedItemTypeIndex >= _compatibleItemTypes.Count)
        {
            _player.SendServerMessage("Invalid item type selected.", ColorConstants.Orange);
            CloseConfirmNoDamageModal();
            return;
        }

        var (newType, newResRef) = _compatibleItemTypes[_selectedItemTypeIndex];
        string currentName = _model.SelectedItem.Name;
        string currentDesc = _model.SelectedItem.Description;
        string currentTag = _model.SelectedItem.Tag;
        Location currentLocation = _model.SelectedItem.Location;

        CloseConfirmNoDamageModal();
        await PerformItemTypeChange(newType, newResRef, currentName, currentDesc, currentTag, currentLocation);
    }

    private void CloseConfirmNoDamageModal()
    {
        if (_confirmNoDamageModalToken.HasValue)
        {
            _confirmNoDamageModalToken.Value.OnNuiEvent -= HandleConfirmNoDamageModalEvent;
            _confirmNoDamageModalToken?.Close();
            _confirmNoDamageModalToken = null;
        }
        _selectedItemTypeIndex = -1;
    }

    private async Task PerformItemTypeChange(BaseItemType newType, string newResRef, string currentName, string currentDesc, string currentTag, Location currentLocation)
    {
        // Create new item with the new base type
        NwItem? newItem = null;

        if (_model.SelectedItem?.Possessor is NwGameObject possessor)
        {
            // Item is in a creature's inventory or container
            newItem = await NwItem.Create(newResRef, possessor, 1, "");
        }
        else
        {
            // Item is on the ground - create with null possessor at the location
            newItem = await NwItem.Create(newResRef, null, 1, "");
            if (newItem != null)
            {
                // Move the item to the original item's location
                newItem.Location = currentLocation;
            }
        }

        if (newItem == null)
        {
            _player.SendServerMessage($"Failed to create item of type {GetItemTypeName(newType)}.", ColorConstants.Red);
            CloseEditItemTypeModal();
            return;
        }

        // Copy over basic properties
        newItem.Name = currentName;
        newItem.Description = currentDesc;
        newItem.Tag = currentTag;

        // Copy all item properties from the old item
        int propertiesCopied = 0;
        foreach (var prop in _model.SelectedItem.ItemProperties)
        {
            newItem.AddItemProperty(prop, EffectDuration.Permanent);
            propertiesCopied++;
        }

        // Copy variables
        int varsCopied = 0;
        foreach (ObjectVariable var in _model.SelectedItem.LocalVariables)
        {
            switch (var)
            {
                case LocalVariableInt li:
                    newItem.GetObjectVariable<LocalVariableInt>(li.Name).Value = li.Value;
                    varsCopied++;
                    break;
                case LocalVariableFloat lf:
                    newItem.GetObjectVariable<LocalVariableFloat>(lf.Name).Value = lf.Value;
                    varsCopied++;
                    break;
                case LocalVariableString ls:
                    newItem.GetObjectVariable<LocalVariableString>(ls.Name).Value = ls.Value ?? string.Empty;
                    varsCopied++;
                    break;
                case LocalVariableLocation lloc:
                    newItem.GetObjectVariable<LocalVariableLocation>(lloc.Name).Value = lloc.Value;
                    varsCopied++;
                    break;
                case LocalVariableObject<NwObject> lo:
                    newItem.GetObjectVariable<LocalVariableObject<NwObject>>(lo.Name).Value = lo.Value;
                    varsCopied++;
                    break;
            }
        }

        // Destroy the old item
        _model.SelectedItem.Destroy();

        // Update the model with the new item
        _model.SelectedItem = newItem;
        UpdateFromModel();

        _player.SendServerMessage($"Item type changed to {GetItemTypeName(newType).ColorString(ColorConstants.Cyan)}. Copied {propertiesCopied} properties and {varsCopied} variables.", ColorConstants.Green);
        CloseEditItemTypeModal();
    }

    private void CloseEditItemTypeModal()
    {
        if (_editItemTypeModalToken.HasValue)
        {
            _editItemTypeModalToken.Value.OnNuiEvent -= HandleEditItemTypeModalEvent;
            _editItemTypeModalToken?.Close();
            _editItemTypeModalToken = null;
        }
        _compatibleItemTypes.Clear();
        _selectedItemTypeIndex = -1;
    }

    private List<(BaseItemType Type, string ResRef)> GetBaseItemTypeMapping(BaseItemType currentType)
    {
        var result = new List<(BaseItemType, string)>();

        // Check if it's armor - special case with AC dropdown
        if (currentType == BaseItemType.Armor)
        {
            return GetArmorAcMappings();
        }

        // 1-Handed Weapons category (including custom items)
        var oneHandedWeapons = new List<(BaseItemType, string)>
        {
            (BaseItemType.Shortsword, "js_bla_wess"),
            (BaseItemType.Longsword, "js_bla_wels"),
            (BaseItemType.Bastardsword, "js_bla_webs"),
            (BaseItemType.Rapier, "js_bla_wera"),
            (BaseItemType.Scimitar, "js_bla_wesc"),
            (BaseItemType.Handaxe, "js_bla_weha"),
            (BaseItemType.Battleaxe, "js_bla_weba"),
            (BaseItemType.Katana, "js_bla_weka"),
            (BaseItemType.LightHammer, "js_bla_welh"),
            (BaseItemType.LightMace, "js_bla_wema"),
            (BaseItemType.Morningstar, "js_bla_wemo"),
            (BaseItemType.Club, "js_bla_wecl"),
            (BaseItemType.Dagger, "js_bla_weda"),
            (BaseItemType.Kama, "js_bla_wekm"),
            (BaseItemType.Kukri, "js_bla_weku"),
            (BaseItemType.Sickle, "js_bla_wesi"),
            (BaseItemType.Warhammer, "js_bla_wewa"),
            (BaseItemType.LightFlail, "js_bla_welf"),
            (BaseItemType.Whip, "js_bla_wewh"),
            (BaseItemType.Trident, "js_bla_wetr"),
            (BaseItemType.DwarvenWaraxe, "js_bla_wedw"),
            (BaseItemType.MagicStaff, "js_bla_wems"),
            // Custom 1H items (row 116-221)
            ((BaseItemType)116, "hldb_book"),        // Tools, Right
            ((BaseItemType)117, "temp_chair"),       // Holdable Chair
            ((BaseItemType)204, "mus_flute"),        // Baby, etc.
            ((BaseItemType)208, "item_flower_a3"),   // Hand Flower
            ((BaseItemType)209, "item_flower_b3"),   // Hand Bouquet
            ((BaseItemType)210, "temp_cflower"),     // Crystal Flower
            ((BaseItemType)211, "temp_cbouquet"),    // Crystal Bouquet
            ((BaseItemType)212, "item_bottle"),      // Drinks/Quill
            ((BaseItemType)213, "temp_trade"),       // Tools, Trade
            ((BaseItemType)214, "temp_htome"),       // Tome, Light
            ((BaseItemType)215, "temp_utome"),       // Tome, Dark
            ((BaseItemType)219, "item_pipe1"),       // Pipes, Spyglass
            ((BaseItemType)221, "temp_cards"),       // Play Cards

            // ⚠ Items below cannot take weapon properties (Keen, Damage Bonus, etc.)
            ((BaseItemType)93, "temp_trumpet"),      // ⚠ Trumpet
            ((BaseItemType)15, "nw_it_torch001"),    // ⚠ Torch
            ((BaseItemType)94, "temp_moon"),         // ⚠ Moon On A Stick
            ((BaseItemType)113, "hldb_bucket"),      // ⚠ Tools, Left
            ((BaseItemType)222, "temp_focus"),       // ⚠ Focus
            ((BaseItemType)223, "temp_umbrella")    // ⚠ Umbrella
        };

        // 2-Handed Weapons category (including custom items)
        var twoHandedWeapons = new List<(BaseItemType, string)>
        {
            (BaseItemType.Greatsword, "js_bla_wegs"),
            (BaseItemType.Greataxe, "js_bla_wega"),
            (BaseItemType.Halberd, "js_bla_wehb"),
            (BaseItemType.HeavyFlail, "js_bla_wehf"),
            (BaseItemType.Scythe, "js_bla_wesy"),
            (BaseItemType.Quarterstaff, "js_bla_wequ"),
            (BaseItemType.ShortSpear, "js_bla_wesp"),
            (BaseItemType.Doubleaxe, "js_bla_wedb"),
            (BaseItemType.TwoBladedSword, "js_bla_we2b"),
            (BaseItemType.DireMace, "js_bla_wedm"),
            // Custom 2H items
            ((BaseItemType)114, "hldb_shovel"),      // Tools, Pole
            ((BaseItemType)220, "temp_2hmstaff"),    // 2H Magic Staff
        };

        // Ranged Weapons category
        var rangedWeapons = new List<(BaseItemType, string)>
        {
            (BaseItemType.Longbow, "js_arch_bow"),
            (BaseItemType.Shortbow, "js_arch_sbow"),
            (BaseItemType.LightCrossbow, "js_arch_lbow"),
            (BaseItemType.HeavyCrossbow, "js_arch_cbow"),
            (BaseItemType.Sling, "js_arch_sling"),
        };

        // Thrown Weapons category
        var thrownWeapons = new List<(BaseItemType, string)>
        {
            (BaseItemType.Dart, "js_arch_dart"),
            (BaseItemType.Shuriken, "js_arch_shrk"),
            (BaseItemType.ThrowingAxe, "js_arch_thax"),
        };

        // Ammunition category
        var ammunition = new List<(BaseItemType, string)>
        {
            (BaseItemType.Arrow, "js_arch_star"),
            (BaseItemType.Bolt, "js_arch_stbt"),
            (BaseItemType.Bullet, "js_arch_stbu"),
        };

        // Shields category
        var shields = new List<(BaseItemType, string)>
        {
            (BaseItemType.SmallShield, "js_bla_shsm"),
            (BaseItemType.LargeShield, "js_bla_shlg"),
            (BaseItemType.TowerShield, "js_bla_shto"),
        };

        // Accessories category
        var accessories = new List<(BaseItemType, string)>
        {
            (BaseItemType.Helmet, "js_bla_helm"),
            (BaseItemType.Amulet, "js_jew_amul"),
            (BaseItemType.Ring, "js_jew_ring"),
            ((BaseItemType)21, "js_tai_belt"),       // Belt
            ((BaseItemType)26, "js_tai_boot"),       // Boots
            ((BaseItemType)36, "js_tai_glove"),      // Gloves
            ((BaseItemType)78, "js_bla_brac"),       // Bracer
            ((BaseItemType)80, "js_tai_cloa"),       // Cloak
        };

        // Miscellaneous category
        var miscellaneous = new List<(BaseItemType, string)>
        {
            ((BaseItemType)24, "dc_cus_temp_s"),     // Misc Small 1
            ((BaseItemType)119, "dc_cus_temp_s2"),   // Misc Small 2
            ((BaseItemType)120, "dc_cus_temp_s3"),   // Misc Small 3
            ((BaseItemType)29, "temp_miscm1"),       // Misc Medium 1
            ((BaseItemType)121, "temp_miscm2"),      // Misc Medium 2
            ((BaseItemType)34, "temp_miscl"),        // Misc Large
            ((BaseItemType)79, "dc_cus_temp_t"),     // Misc Thin
            ((BaseItemType)74, "temp_book"),         // Book
            ((BaseItemType)77, "nw_it_gem013"),      // Gem
            ((BaseItemType)118, "temp_gem2"),        // Gem 2
            ((BaseItemType)122, "temp_medal"),       // Medals
            ((BaseItemType)123, "temp_fstone"),      // Faerie Stone
            ((BaseItemType)124, "temp_ioun"),        // Ioun Stone
        };

        // Determine which category the current item belongs to and return those options
        if (oneHandedWeapons.Any(w => w.Item1 == currentType))
            return oneHandedWeapons;
        if (twoHandedWeapons.Any(w => w.Item1 == currentType))
            return twoHandedWeapons;
        if (rangedWeapons.Any(w => w.Item1 == currentType))
            return rangedWeapons;
        if (thrownWeapons.Any(w => w.Item1 == currentType))
            return thrownWeapons;
        if (ammunition.Any(w => w.Item1 == currentType))
            return ammunition;
        if (shields.Any(w => w.Item1 == currentType))
            return shields;
        if (accessories.Any(w => w.Item1 == currentType))
            return accessories;
        if (miscellaneous.Any(w => w.Item1 == currentType))
            return miscellaneous;

        return result;
    }

    private List<(BaseItemType Type, string ResRef)> GetArmorAcMappings()
    {
        // Special handling for armor - return AC 0 to AC 8 mappings
        return new List<(BaseItemType, string)>
        {
            ((BaseItemType)0, "js_tai_arcl"),   // AC 0
            ((BaseItemType)1, "js_tai_arpa"),   // AC 1
            ((BaseItemType)2, "js_tai_arle"),   // AC 2
            ((BaseItemType)3, "js_tai_arha"),   // AC 3
            ((BaseItemType)4, "js_bla_arsc"),   // AC 4
            ((BaseItemType)5, "js_bla_arch"),   // AC 5
            ((BaseItemType)6, "js_bla_arbm"),   // AC 6
            ((BaseItemType)7, "js_bla_arhp"),   // AC 7
            ((BaseItemType)8, "js_bla_arfp"),   // AC 8
        };
    }

    private string GetItemTypeName(BaseItemType itemType)
    {
        // First check if it's a standard enum value with a display name
        string standardName = itemType.ToString();

        // Handle custom base item types (rows 93+)
        string customName = (int)itemType switch
        {
            93 => "Trumpet",
            94 => "Moon On A Stick",
            113 => "Tools, Left (Bucket)",
            114 => "Tools, Pole (Shovel)",
            116 => "Tools, Right",
            117 => "Holdable Chair",
            118 => "Gem 2",
            119 => "Misc Small 2",
            120 => "Misc Small 3",
            121 => "Misc Medium 2",
            122 => "Medals",
            123 => "Faerie Stone",
            124 => "Ioun Stone",
            204 => "Baby, etc.",
            208 => "Hand Flower",
            209 => "Hand Bouquet",
            210 => "Crystal Flower",
            211 => "Crystal Bouquet",
            212 => "Drinks/Quill",
            213 => "Tools, Trade",
            214 => "Tome, Light",
            215 => "Tome, Dark",
            219 => "Pipes, Spyglass",
            220 => "2H Magic Staff",
            221 => "Play Cards",
            222 => "Focus",
            223 => "Umbrella",
            _ => null
        };

        if (customName != null)
            return customName;

        // For standard enums, convert to friendly display name
        return System.Text.RegularExpressions.Regex.Replace(standardName, "([a-z])([A-Z])", "$1 $2");
    }

    /// <summary>
    /// Checks if an item has weapon-specific properties that would not transfer to [NO DAMAGE] item types.
    /// </summary>
    private bool HasWeaponProperties(NwItem item)
    {
        if (item == null)
            return false;

        // Check if the item has any item properties at all
        // If it does, we assume they might be weapon-specific and block the change to be safe
        // This is a conservative approach that prevents data loss
        int propertyCount = 0;
        foreach (var itemProp in item.ItemProperties)
        {
            propertyCount++;
            // If there are any item properties, assume they're weapon-specific
            // This is safer than trying to identify specific property types
            return true;
        }

        return propertyCount > 0;
    }
}


