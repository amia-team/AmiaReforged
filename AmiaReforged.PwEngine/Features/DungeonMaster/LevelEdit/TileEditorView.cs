using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using System.Linq;
using System.Numerics;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.LevelEdit;

public sealed class TileEditorView : ScryView<TileEditorPresenter>, IDmWindow
{
    public override TileEditorPresenter Presenter { get; protected set; }

    public string Title => "Tile Editor";
    public bool ListInDmTools => false;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    // Minimal binds to allow selection
    public readonly NuiBind<bool> TileIsSelected = new("tile_is_selected");
    public readonly NuiBind<string> TileId = new("tile_id");
    public readonly NuiBind<string> TileRotation = new("tile_rotation");
    public readonly NuiBind<bool> LiveUpdateEnabled = new("tile_live_update");
    public readonly NuiBind<string> LiveUpdateText = new("tile_live_update_text");

    public NuiButton PickATileButton = null!;
    public NuiButton SaveTileButton = null!;
    public NuiButton PickNorthTile = null!;
    public NuiButton PickRightTile = null!;
    public NuiButton PickLeftTile = null!;
    public NuiButton PickSouthTile = null!;
    public NuiButton RotateOrientationCounter = null!;
    public NuiButton RotateOrientationClockwise = null!;
    public NuiButton TileIdDecButton = null!;
    public NuiButton TileIdIncButton = null!;
    public NuiButton ToggleLiveButton = null!;

    public TileEditorView(NwPlayer player)
    {
        Presenter = new TileEditorPresenter(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        return new NuiGroup
        {
            Element = new NuiColumn
            {
                Width = 380f,
                Children =
                [
                    // Tile info row
                    new NuiRow
                    {
                        Children =
                        [
                            new NuiLabel("Tile ID") { Width = 80f, VerticalAlign = NuiVAlign.Middle },
                            new NuiButton("<") { Id = "tileid_dec", Enabled = TileIsSelected, Width = 26f, Height = 26f }.Assign(out TileIdDecButton),
                            new NuiTextEdit("", TileId, 5, false) { Enabled = TileIsSelected, Width = 50f },
                            new NuiButton(">") { Id = "tileid_inc", Enabled = TileIsSelected, Width = 26f, Height = 26f }.Assign(out TileIdIncButton)
                        ]
                    },
                    new NuiRow
                    {
                        Children =
                        [
                            new NuiLabel("Rotation") { Width = 80f, VerticalAlign = NuiVAlign.Middle },
                            new NuiGroup
                            {
                                Width = 100f, Height = 26f,
                                Element = new NuiLabel(TileRotation)
                                    { VerticalAlign = NuiVAlign.Middle, HorizontalAlign = NuiHAlign.Center }
                            },
                            new NuiButton("<")
                                    { Id = "rotate_counter", Enabled = TileIsSelected, Width = 26f, Height = 26f }
                                .Assign(out RotateOrientationCounter),
                            new NuiButton(">")
                                    { Id = "rotate_clockwise", Enabled = TileIsSelected, Width = 26f, Height = 26f }
                                .Assign(out RotateOrientationClockwise)
                        ]
                    },
                    new NuiRow
                    {
                        Children =
                        [
                            new NuiLabel("Live Update") { Width = 80f, VerticalAlign = NuiVAlign.Middle },
                            new NuiLabel(LiveUpdateText) { Width = 60f, VerticalAlign = NuiVAlign.Middle },
                            new NuiButton("Toggle") { Id = "btn_toggle_live", Height = 24f, Width = 60f }.Assign(out ToggleLiveButton)
                        ]
                    },

                    // Directional controls similar to AreaEditorView
                    new NuiLabel("Pick Adjacent Tile") { Height = 15f },
                    new NuiRow
                    {
                        Children =
                        [
                            new NuiSpacer { Width = 50f },
                            new NuiButton("North")
                            {
                                Id = "up_button",
                                Width = 50f,
                                Height = 36f,
                                Enabled = TileIsSelected
                            }.Assign(out PickNorthTile)
                        ]
                    },
                    new NuiRow
                    {
                        Children =
                        [
                            new NuiButton("West")
                            {
                                Id = "left_button",
                                Width = 50f,
                                Height = 36f,
                                Enabled = TileIsSelected
                            }.Assign(out PickLeftTile),
                            new NuiSpacer { Width = 50f },
                            new NuiButton("East")
                            {
                                Id = "right_button",
                                Width = 50f,
                                Height = 36f,
                                Enabled = TileIsSelected
                            }.Assign(out PickRightTile)
                        ]
                    },
                    new NuiRow
                    {
                        Children =
                        [
                            new NuiSpacer { Width = 50f },
                            new NuiButton("South")
                            {
                                Id = "down_button",
                                Width = 50f,
                                Height = 36f,
                                Enabled = TileIsSelected
                            }.Assign(out PickSouthTile)
                        ]
                    },

                    new NuiRow
                    {
                        Children =
                        [
                            new NuiButton("Pick Tile") { Id = "btn_pick_tile" }.Assign(out PickATileButton),
                            new NuiButton("Save Tile") { Id = "btn_save_tile", Enabled = TileIsSelected }.Assign(
                                out SaveTileButton)
                        ]
                    }
                ]
            }
        };
    }
}

public sealed class TileEditorPresenter : ScryPresenter<TileEditorView>
{
    public override TileEditorView View { get; }

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    [Inject] private Lazy<LevelEditorService>? LevelEditorService { get; init; }

    private LevelEditSession? _session;
    private Location? _selectedLocation;
    private TileRotation _selectedRotation = TileRotation.Rotate0;
    private bool _updatingUi = false;
    private bool _tileIdDirty = false;

    public TileEditorPresenter(TileEditorView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Tile Editor") { Geometry = new NuiRect(0f, 100f, 420f, 320f) };
    }

    public override void Create()
    {
        if (_window is null) InitBefore();
        if (_window is null) return;

        _player.TryCreateNuiWindow(_window, out _token);

        NwArea? area = _player.LoginCreature?.Area;
        if (area is null) return;

        _session = LevelEditorService?.Value.GetOrCreateSessionForArea(area);
        _session?.RegisterPresenter(View.Presenter);

        // Ensure session state has a selected area so tileset data resolves correctly
        if (_session?.State is not null && _session.State.SelectedArea is null)
        {
            _session.State.SelectedArea = area;
        }

        Token().SetBindValue(View.TileIsSelected, false);
        Token().SetBindValue(View.LiveUpdateEnabled, true);
        Token().SetBindValue(View.LiveUpdateText, "On");

        Token().SetBindWatch(View.TileId, true);
        Token().SetBindValue(View.TileId, Token().GetBindValue(View.TileId) ?? "0");

        LoadFromSession();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        // Sanitize tile id live as the user types
        if (obj.EventType == NuiEventType.Watch && obj.ElementId == View.TileId.Key)
        {
            if (_updatingUi)
            {
                // Ignore watch fired by our own programmatic SetBindValue
                return;
            }

            _tileIdDirty = true;
            SanitizeTileId();
            return;
        }

        if (obj.EventType != NuiEventType.Click) return;

        if (obj.ElementId == View.PickATileButton.Id)
        {
            StartTilePicker();
            return;
        }

        if (obj.ElementId == View.TileIdDecButton.Id)
        {
            AdjustTileId(-1);
            return;
        }

        if (obj.ElementId == View.TileIdIncButton.Id)
        {
            AdjustTileId(+1);
            return;
        }

        if (obj.ElementId == View.ToggleLiveButton.Id)
        {
            bool current = Token().GetBindValue(View.LiveUpdateEnabled);
            bool next = !current;
            Token().SetBindValue(View.LiveUpdateEnabled, next);
            Token().SetBindValue(View.LiveUpdateText, next ? "On" : "Off");
            return;
        }

        if (obj.ElementId == View.RotateOrientationCounter.Id)
        {
            _selectedRotation = PreviousRotation(_selectedRotation);
            Token().SetBindValue(View.TileRotation, _selectedRotation.ToString());
            MaybeLiveApply();
            return;
        }

        if (obj.ElementId == View.RotateOrientationClockwise.Id)
        {
            _selectedRotation = NextRotation(_selectedRotation);
            Token().SetBindValue(View.TileRotation, _selectedRotation.ToString());
            MaybeLiveApply();
            return;
        }

        if (obj.ElementId == View.PickNorthTile.Id)
        {
            PickNeighbor(Direction.North);
            return;
        }

        if (obj.ElementId == View.PickLeftTile.Id)
        {
            PickNeighbor(Direction.West);
            return;
        }

        if (obj.ElementId == View.PickRightTile.Id)
        {
            PickNeighbor(Direction.East);
            return;
        }

        if (obj.ElementId == View.PickSouthTile.Id)
        {
            PickNeighbor(Direction.South);
            return;
        }

        if (obj.ElementId == View.SaveTileButton.Id)
        {
            ApplyChanges();
            return;
        }
    }

    private void AdjustTileId(int delta)
    {
        if (_selectedLocation is null) return;
        string raw = Token().GetBindValue(View.TileId) ?? string.Empty;
        string digits = new string(raw.Where(char.IsDigit).ToArray());
        int value = _selectedLocation.TileId;
        if (digits.Length > 0 && int.TryParse(digits, out int parsed)) value = parsed;
        int cap = GetMaxTileCap();
        value += delta;
        if (value < 0) value = 0;
        if (cap > 0 && value > cap) value = cap;
        _updatingUi = true;
        Token().SetBindValue(View.TileId, value.ToString());
        _updatingUi = false;
        _tileIdDirty = true; // user intent
        MaybeLiveApply();
    }

    private void MaybeLiveApply()
    {
        if (_selectedLocation is null) return;
        bool live = Token().GetBindValue(View.LiveUpdateEnabled);
        if (!live) return;
        ApplyChanges();
    }

    private void StartTilePicker()
    {
        NwArea? area = _session?.State.SelectedArea ?? _player.LoginCreature?.Area;
        if (area is null) return;

        _player.EnterTargetMode(OnTilePicked, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Tile
        });
    }

    private void OnTilePicked(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.Player != _player) return;
        if (obj.Player.LoginCreature?.Area is null) return;

        Location loc = Location.Create(obj.Player.LoginCreature.Area, obj.TargetPosition, 0);

        if (loc.TileInfo is not null)
        {
            _selectedLocation = loc;
            _selectedRotation = (TileRotation)loc.TileInfo.Orientation;
            UpdateUiFromSelection();
        }
    }

    private void UpdateUiFromSelection()
    {
        if (_selectedLocation?.TileInfo is null) return;

        _updatingUi = true;
        Token().SetBindValue(View.TileId, _selectedLocation.TileId.ToString());
        _updatingUi = false;
        _tileIdDirty = false;
        Token().SetBindValue(View.TileRotation, _selectedRotation.ToString());
        Token().SetBindValue(View.TileIsSelected, true);
    }

    private void ApplyChanges()
    {
        if (_selectedLocation is null) return;

        string? tileIdStr = Token().GetBindValue(View.TileId);
        int tileId;
        if (!_tileIdDirty)
        {
            // User didn't edit the field; use the actual selected tile's id
            tileId = _selectedLocation.TileId;
        }
        else if (!int.TryParse(tileIdStr, out tileId))
        {
            tileId = _selectedLocation.TileId;
        }

        // Clamp once more on save for safety
        int maxTile = GetMaxTileCap();
        if (tileId < 0) tileId = 0;
        if (maxTile > 0 && tileId > maxTile) tileId = maxTile;

        int tileHeight = _selectedLocation.TileHeight;

        _selectedLocation.SetTile(tileId, _selectedRotation, tileHeight);
        _player.SendServerMessage($"Applied tile changes: ID {tileId}, Rot {_selectedRotation}.");
    }

    private int GetMaxTileCap()
    {
        try
        {
            NwArea? tilesetArea = _selectedLocation?.Area
                                  ?? _session?.State.SelectedArea
                                  ?? _player.LoginCreature?.Area;
            if (tilesetArea is not null)
            {
                int n = TilesetPlugin.GetTilesetData(tilesetArea.Tileset).nNumTileData;
                if (n > 0) return n;
            }
        }
        catch { /* ignore and fall back */ }
        return -1;
    }

    private void PickNeighbor(Direction d)
    {
        if (_selectedLocation?.TileInfo is null) return;

        IReadOnlyList<Anvil.API.TileInfo> areaTileInfo = _selectedLocation.Area.TileInfo;

        int? currentX = _selectedLocation.TileInfo?.GridX;
        int? currentY = _selectedLocation.TileInfo?.GridY;
        if (currentX is null || currentY is null) return;

        int dx = 0, dy = 0;
        switch (d)
        {
            case Direction.North: dy = 1; break;
            case Direction.South: dy = -1; break;
            case Direction.West: dx = -1; break;
            case Direction.East: dx = 1; break;
        }

        int nx = currentX.Value + dx;
        int ny = currentY.Value + dy;

        int width = _selectedLocation.Area.Size.X;
        int height = _selectedLocation.Area.Size.Y;
        bool inBounds = nx >= 0 && nx < width && ny >= 0 && ny < height;
        if (!inBounds)
        {
            _player.SendServerMessage("You're at the edge of the area!");
            return;
        }

        Anvil.API.TileInfo? neighbor = areaTileInfo.FirstOrDefault(t => t.GridX == nx && t.GridY == ny);
        if (neighbor is null) return;

        Vector3 center = GetTileCenter(neighbor);
        _selectedLocation = Location.Create(_selectedLocation.Area, center, 0f);
        _selectedRotation = (TileRotation)neighbor.Orientation;
        UpdateUiFromSelection();
    }

    private static Vector3 GetTileCenter(Anvil.API.TileInfo tileInfo)
    {
        const float tileSize = 10.0f;
        const float half = tileSize / 2.0f;
        float cx = (tileInfo.GridX * tileSize) + half;
        float cy = (tileInfo.GridY * tileSize) + half;
        return new Vector3(cx, cy, 0f);
    }

    private static TileRotation NextRotation(TileRotation rotation)
    {
        int next = ((int)rotation + 1) % Enum.GetValues<TileRotation>().Length;
        return (TileRotation)next;
    }

    private static TileRotation PreviousRotation(TileRotation rotation)
    {
        int prev = ((int)rotation - 1 + Enum.GetValues<TileRotation>().Length)
                   % Enum.GetValues<TileRotation>().Length;
        return (TileRotation)prev;
    }

    private void SanitizeTileId()
    {
        // Read current text and keep only digits
        string raw = Token().GetBindValue(View.TileId) ?? string.Empty;
        string digits = new string(raw.Where(char.IsDigit).ToArray());
        // If user cleared the field or typed non-digits, don't force 0; let save fall back to selected tile id
        if (digits.Length == 0)
        {
            if (raw != digits)
            {
                _updatingUi = true;
                Token().SetBindValue(View.TileId, digits);
                _updatingUi = false;
            }
            // Don't attempt live apply on empty field
            return;
        }

        // Parse to int and clamp to [0, maxTile]
        if (!int.TryParse(digits, out int value)) value = 0;
        if (value < 0) value = 0;

        int maxTile = GetMaxTileCap();

        if (maxTile > 0 && value > maxTile) value = maxTile;

        // Write sanitized value back to the bind
        _updatingUi = true;
        Token().SetBindValue(View.TileId, value.ToString());
        _updatingUi = false;

        // Apply live after user typed a valid numeric value
        MaybeLiveApply();
    }

    private void LoadFromSession()
    {
        if (_session is null) return;
        if (_session.State.SelectedArea is null) return;
        // no-op for now
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

    private enum Direction
    {
        North,
        South,
        West,
        East
    }
}
