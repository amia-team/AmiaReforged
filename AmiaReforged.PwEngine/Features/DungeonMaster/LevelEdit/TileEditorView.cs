using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

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

    public NuiButton PickATileButton = null!;
    public NuiButton SaveTileButton = null!;

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
                Width = 320f,
                Children =
                [
                    new NuiRow { Children = [ new NuiLabel("Tile ID"), new NuiTextEdit("", TileId, 5, false) ]},
                    new NuiRow { Children = [ new NuiLabel("Rotation"), new NuiLabel(TileRotation) ]},
                    new NuiRow { Children = [ new NuiButton("Pick Tile") { Id = "btn_pick_tile" }.Assign(out PickATileButton), new NuiButton("Save Tile") { Id = "btn_save_tile" }.Assign(out SaveTileButton) ]}
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

    public TileEditorPresenter(TileEditorView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Tile Editor") { Geometry = new NuiRect(0f, 100f, 340f, 220f) };
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

        LoadFromSession();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click) return;

        if (obj.ElementId == View.PickATileButton.Id)
        {
            // delegate to Tile selection logic from AreaEditorPresenter's TileEditorHandler eventually
        }

        if (obj.ElementId == View.SaveTileButton.Id)
        {
            // apply changes to the underlying session's AreaEditorState / TileSelection
        }
    }

    private void LoadFromSession()
    {
        if (_session is null) return;
        if (_session.State.SelectedArea is null) return;

        // We could populate selected tile + values here if the session stores them
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
