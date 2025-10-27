using AmiaReforged.Core.Models.DmModels;
using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.LevelEdit;

public sealed class SavedInstancesView : ScryView<SavedInstancesPresenter>, IDmWindow
{
    public override SavedInstancesPresenter Presenter { get; protected set; }

    public string Title => "Saved Instances";
    public bool ListInDmTools => false;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    // Binds
    public readonly NuiBind<string> SavedVariantNames = new("saved_variant_names");
    public readonly NuiBind<int> SavedVariantCounts = new("saved_variant_count");
    public readonly NuiBind<string> NewAreaName = new("new_area_name");
    public readonly NuiBind<bool> CanSaveArea = new("can_save_area");
    public readonly NuiBind<string> CreatedAtBind = new("created_at");
    public readonly NuiBind<string> UpdatedAtBind = new("updated_at");

    public NuiButton SaveNewInstanceButton = null!;

    public SavedInstancesView(NwPlayer player)
    {
        Presenter = new SavedInstancesPresenter(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> savedInstances =
        [
            new(new NuiLabel(SavedVariantNames)) { Width = 160f },
            new(new NuiLabel(CreatedAtBind)) { Width = 110f },
            new(new NuiLabel(UpdatedAtBind)) { Width = 110f },
            new(new NuiButtonImage("ir_edit")
            {
                Id = "btn_rename_var",
                Aspect = 1f,
                Tooltip = "Rename this instance (uses text field above)"
            })
            {
                Width = 30f,
                VariableSize = false
            },
            new(new NuiButtonImage("ir_abort")
            {
                Id = "btn_delete_var",
                Aspect = 1f,
                Tooltip = "Delete this instance"
            })
            {
                Width = 30f,
                VariableSize = false
            },
            new(new NuiButtonImage("dm_goto")
            {
                Id = "btn_load_var",
                Aspect = 1f,
                Tooltip = "Load this instance (spawns new area)"
            })
            {
                Width = 30f,
                VariableSize = false
            },
            new(new NuiButtonImage("dm_jumpto")
            {
                Id = "btn_quickload_var",
                Aspect = 1f,
                Tooltip = "Quick-load and teleport DM to the spawned area"
            })
            {
                Width = 30f,
                VariableSize = false
            }
        ];

        // Pre-create the save button and store reference on the view
        NuiButton saveBtn = new NuiButton("Save New Instance")
        {
            Id = "btn_save_instance",
            Height = 30f,
            Enabled = CanSaveArea,
            DisabledTooltip = "You cannot create copies of a copy."
        };
        SaveNewInstanceButton = saveBtn;

        return new NuiColumn
        {
            Children =
            [
                new NuiRow
                {
                    Children =
                    [
                        new NuiTextEdit("Type a name...", NewAreaName, 255, false)
                        {
                            Height = 30f,
                            Enabled = CanSaveArea
                        },
                        new NuiSpacer(),
                        saveBtn
                    ]
                },
                new NuiList(savedInstances, SavedVariantCounts)
                {
                    Width = 540f,
                    Height = 220f
                }
            ]
        };
    }
}

public sealed class SavedInstancesPresenter : ScryPresenter<SavedInstancesView>
{
    public override SavedInstancesView View { get; }

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    private LevelEditSession? _session;

    [Inject] private Lazy<LevelEditorService>? LevelEditorService { get; init; }
    [Inject] private Lazy<DmAreaService>? AreaService { get; init; }
    [Inject] private Lazy<WindowDirector>? WindowDirector { get; init; }

    private readonly List<DmArea> _rows = new();
    private readonly List<string> _createdStrings = new();
    private readonly List<string> _updatedStrings = new();

    public SavedInstancesPresenter(SavedInstancesView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 360f, 300f)
        };
    }

    public override void Create()
    {
        if (_window is null) InitBefore();
        if (_window is null)
        {
            _player.SendServerMessage("The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Attach to session for current area
        _session = LevelEditorService?.Value.GetOrCreateSessionForArea(_player.LoginCreature!.Area!);
        _session?.RegisterPresenter(View.Presenter);

        // Can save? Not if this area is itself an instance (i.e., has is_instance local int TRUE)
        bool canSave = _session?.State.CanSaveArea ?? false;
        Token().SetBindValue(View.CanSaveArea, canSave);

        RefreshInstanceList();
    }

    private void RefreshInstanceList()
    {
        if (_session?.Area is null)
        {
            Token().SetBindValue(View.SavedVariantCounts, 0);
            Token().SetBindValues(View.SavedVariantNames, new List<string>());
            Token().SetBindValues(View.CreatedAtBind, new List<string>());
            Token().SetBindValues(View.UpdatedAtBind, new List<string>());
            return;
        }

        string resref = _session.Area.ResRef;
        List<DmArea> rows = AreaService!.Value.AllFromResRef(_player.CDKey, resref);
        _rows.Clear();
        _rows.AddRange(rows);

        List<string> names = rows.Select(a => a.NewName).ToList();
        _createdStrings.Clear();
        _updatedStrings.Clear();
        foreach (DmArea r in rows)
        {
            _createdStrings.Add(r.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
            _updatedStrings.Add(r.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        }

        Token().SetBindValue(View.SavedVariantCounts, names.Count);
        Token().SetBindValues(View.SavedVariantNames, names);
        Token().SetBindValues(View.CreatedAtBind, _createdStrings);
        Token().SetBindValues(View.UpdatedAtBind, _updatedStrings);

        // stash list in session state for index mapping if needed
        _session.State.SavedInstances = rows;
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        switch (obj.EventType)
        {
            case NuiEventType.Click:
                HandleClick(obj);
                break;
        }
    }

    private void HandleClick(ModuleEvents.OnNuiEvent evt)
    {
        if (_session?.Area is null) return;

        if (evt.ElementId == View.SaveNewInstanceButton.Id)
        {
            string? newInstanceName = Token().GetBindValue(View.NewAreaName);
            if (string.IsNullOrWhiteSpace(newInstanceName))
            {
                _player.SendServerMessage("Name Input Cannot Be Empty");
                return;
            }

            if (!_session.State.CanSaveArea && IsCurrentAreaInstance(_session.Area))
            {
                // Confirm overwrite
                WindowDirector?.Value.OpenPopupWithReaction(_player, "Overwrite Instance?",
                    $"This area is an instance. Overwrite the record named '{newInstanceName}' with current area state?",
                    outcome: () =>
                    {
                        SaveOverExistingInstance(newInstanceName);
                        RefreshInstanceList();
                    },
                    ignoreButton: false, linkedToken: Token());
                return;
            }

            DmArea? existing = AreaService!.Value.InstanceFromKey(_player.CDKey, _session.Area.ResRef, newInstanceName);
            if (existing is null)
            {
                TryPersistCurrentAreaAsNew(newInstanceName);
                RefreshInstanceList();
                return;
            }

            WindowDirector?.Value.OpenPopup(_player, "Duplicate Name", "An instance with that name already exists.",
                false);
            return;
        }

        if (evt.ElementId == "btn_rename_var")
        {
            string? newName = Token().GetBindValue(View.NewAreaName);
            if (string.IsNullOrWhiteSpace(newName))
            {
                _player.SendServerMessage("Type a new name in the field before renaming.");
                return;
            }

            RenameInstance(evt.ArrayIndex, newName);
            RefreshInstanceList();
            return;
        }

        if (evt.ElementId == "btn_load_var")
        {
            LoadInstance(evt.ArrayIndex, teleport: false);
            RefreshInstanceList();
            return;
        }

        if (evt.ElementId == "btn_quickload_var")
        {
            LoadInstance(evt.ArrayIndex, teleport: true);
            RefreshInstanceList();
            return;
        }

        if (evt.ElementId == "btn_delete_var")
        {
            // Confirm deletion
            int idx = evt.ArrayIndex;
            WindowDirector?.Value.OpenPopupWithReaction(_player, "Delete Instance?",
                $"Delete saved instance? This cannot be undone.",
                outcome: () =>
                {
                    DeleteInstance(idx);
                    RefreshInstanceList();
                }, ignoreButton: false, linkedToken: Token());
            return;
        }
    }

    private bool IsCurrentAreaInstance(NwArea area)
    {
        return NWN.Core.NWScript.GetLocalInt(area, "is_instance") == NWN.Core.NWScript.TRUE;
    }

    private void SaveOverExistingInstance(string name)
    {
        if (_session?.Area is null) return;

        byte[]? are = _session.Area.SerializeARE();
        byte[]? git = _session.Area.SerializeGIT();
        if (are is null || git is null)
        {
            _player.SendServerMessage("Failed to serialize area.");
            return;
        }

        // Find existing by composite key (cdkey/originalresref/name)
        DmArea? existing = AreaService!.Value.InstanceFromKey(_player.CDKey, _session.Area.ResRef, name);
        if (existing is null)
        {
            WindowDirector?.Value.OpenPopup(_player, "Not Found", "No existing instance with that name to overwrite.",
                false);
            return;
        }

        // Update serialized blobs and save
        existing.SerializedARE = are;
        existing.SerializedGIT = git;
        AreaService.Value.SaveArea(existing);
        _player.SendServerMessage($"Updated instance '{name}'.");
    }

    private void TryPersistCurrentAreaAsNew(string name)
    {
        if (_session?.Area is null) return;

        byte[]? are = _session.Area.SerializeARE();
        byte[]? git = _session.Area.SerializeGIT();
        if (are is null || git is null)
        {
            _player.SendServerMessage("Failed to serialize area.");
            return;
        }

        DmArea newInstance = new()
        {
            CdKey = _player.CDKey,
            OriginalResRef = _session.Area.ResRef,
            NewName = name,
            SerializedARE = are,
            SerializedGIT = git
        };

        AreaService!.Value.SaveNew(newInstance);
        _player.SendServerMessage($"Saved new instance '{name}'.");
    }

    private void RenameInstance(int index, string newName)
    {
        if (_session is null || _session.State.SavedInstances.Count <= index)
        {
            _player.SendServerMessage("Invalid selection.");
            return;
        }

        // Prevent duplicate names for same cdkey/resref
        if (AreaService!.Value.InstanceFromKey(_player.CDKey, _session.Area!.ResRef, newName) is not null)
        {
            WindowDirector?.Value.OpenPopup(_player, "Duplicate Name", "Another instance already has that name.",
                false);
            return;
        }

        DmArea dmArea = _session.State.SavedInstances[index];
        dmArea.NewName = newName;
        AreaService.Value.SaveArea(dmArea);
        _player.SendServerMessage($"Renamed instance to '{newName}'.");
    }

    private void LoadInstance(int index, bool teleport)
    {
        if (_session is null || _session.State.SavedInstances.Count <= index)
        {
            _player.SendServerMessage("Invalid selection.");
            return;
        }

        DmArea dmArea = _session.State.SavedInstances[index];
        // Spawn a new area from serialized data
        NwArea? spawned = NwArea.Deserialize(
            dmArea.SerializedARE,
            dmArea.SerializedGIT,
            $"{_player.CDKey}_{dmArea.OriginalResRef}_{dmArea.Id}",
            $"{dmArea.NewName}"
        );

        if (spawned is null)
        {
            _player.SendServerMessage("Failed to load instance.");
            return;
        }

        // Name and Tag must match saved instance name and unique tag format
        spawned.Name = dmArea.NewName;
        spawned.Tag = $"{_player.CDKey}_{dmArea.OriginalResRef}_{dmArea.Id}";

        _player.SendServerMessage($"Spawned instance '{dmArea.NewName}'.");

        if (teleport)
        {
            // Teleport DM to the center of the spawned area
            float x = spawned.Size.X * 10f / 2f + 5f;
            float y = spawned.Size.Y * 10f / 2f + 5f;
            Location loc = Location.Create(spawned, new System.Numerics.Vector3(x, y, 0f), 0f);
            if (_player.LoginCreature is not null)
            {
                _player.LoginCreature.ActionJumpToLocation(loc);
            }

            _player.SendServerMessage("Teleported to spawned instance.");
        }
    }

    private void DeleteInstance(int index)
    {
        if (_session is null || _session.State.SavedInstances.Count <= index)
        {
            _player.SendServerMessage("Invalid selection.");
            return;
        }

        DmArea dmArea = _session.State.SavedInstances[index];
        AreaService!.Value.Delete(dmArea);
        _player.SendServerMessage($"Deleted instance '{dmArea.NewName}'.");
    }

    public override void Close()
    {
        if (_session != null)
        {
            _session.UnregisterPresenter(View.Presenter);
            _session = null;
        }

        try
        {
            _token.Close();
        }
        catch
        {
            // ignore
        }
    }
}
