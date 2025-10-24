using AmiaReforged.PwEngine.Features.DungeonMaster.LevelEdit.AreaEdit;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.DungeonMaster.PlcEdit;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.LevelEdit;

/// <summary>
/// Lightweight "Level Editor" toolbar view --- hosts buttons that open the Area Editor, tools, and help.
/// The heavy tile-editing UI is intentionally left out and will be split into its own view/presenter.
/// </summary>
public sealed class LevelEditView : ScryView<LevelEditPresenter>, IDmWindow
{
    public override LevelEditPresenter Presenter { get; protected set; }

    public string Title => "Area Settings & Selection";
    public bool ListInDmTools => true;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    // Buttons exposed for the presenter to wire up
    public NuiButton AreaSelectorButton = null!;
    public NuiButton AreaSettingsButton = null!;
    // Tools dropdown and open button
    public readonly NuiBind<int> ToolsSelected = new("tools_selected");
    public NuiCombo ToolsCombo = null!;
    public NuiButton OpenToolButton = null!;
    public NuiButton HelpButton = null!;

    public LevelEditView(NwPlayer player)
    {
        Presenter = new LevelEditPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        // Simple horizontal toolbar with four buttons. The actual NUI layout and sizes can be tweaked later.

        // Create elements explicitly to avoid ambiguous extension method resolution for Assign()
        NuiButton areaSelectorButton = new NuiButton("Area Selector") { Id = "btn_area_selector", Height = 30f };
        AreaSelectorButton = areaSelectorButton;

        NuiButton areaSettingsButton = new NuiButton("Area Settings") { Id = "btn_area_settings", Height = 30f };
        AreaSettingsButton = areaSettingsButton;

        // Tools combo and open button
        NuiCombo toolsCombo = Core.UserInterface.NuiUtils.CreateComboForEnum<LevelTool>(ToolsSelected);
        toolsCombo.Width = 140f;
        ToolsCombo = toolsCombo;

        NuiButton openToolButton = new NuiButton("Open") { Id = "btn_open_tool", Height = 30f };
        OpenToolButton = openToolButton;

        NuiButton helpButton = new NuiButton("Help") { Id = "btn_help", Height = 30f };
        HelpButton = helpButton;

        return new NuiGroup
        {
            Element = new NuiRow
            {
                Height = 40f,
                Children =
                [
                    areaSelectorButton,

                    new NuiSpacer { Width = 10f },

                    areaSettingsButton,

                    new NuiSpacer { Width = 10f },

                    // Tools dropdown + open button
                    new NuiGroup
                    {
                        Element = new NuiRow
                        {
                            Children =
                            [
                                toolsCombo,
                                new NuiSpacer { Width = 6f },
                                openToolButton
                            ]
                        }
                    },

                    new NuiSpacer { Width = 10f },

                    helpButton
                ]
            }
        };
    }
}

public sealed class LevelEditPresenter : ScryPresenter<LevelEditView>
{
    public override LevelEditView View { get; }

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    [Inject] private Lazy<WindowDirector>? WindowDirector { get; init; }

    public LevelEditPresenter(LevelEditView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 420f, 60f)
        };
    }

    public override void Create()
    {
        if (_window is null) InitBefore();
        if (_window is null)
        {
            _player.SendServerMessage("The window could not be created. Screenshot this message and report it to a DM.", ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click) return;

        switch (obj.ElementId)
        {
            case var id when id == View.AreaSelectorButton.Id:
                OpenAreaEditor(LevelEditorMode.Selector);
                break;
            case var id when id == View.AreaSettingsButton.Id:
                OpenAreaEditor(LevelEditorMode.Settings);
                break;
            case var id when id == View.OpenToolButton.Id:
                HandleOpenTool();
                break;
             case var id when id == View.HelpButton.Id:
                 WindowDirector?.Value.OpenPopup(_player, "Level Editor Help", "Use Area Selector to pick an area, Area Settings to modify fog/music and save instances. Tile editing has been moved to a separate tool.", false);
                 break;
         }
     }

    private void OpenAreaEditor(LevelEditorMode mode)
     {
         // Open the appropriate editor view based on mode
         switch (mode)
         {
             case LevelEditorMode.Selector:
                 // Open the full Area Editor for area selection
                 var areaEditorView = new AreaEditorView(_player);
                 WindowDirector?.Value.OpenWindow(areaEditorView.Presenter);
                 break;

             case LevelEditorMode.Settings:
                 // Open the dedicated Area Settings view
                 var settingsView = new AreaSettingsView(_player);
                 WindowDirector?.Value.OpenWindow(settingsView.Presenter);
                 break;

             case LevelEditorMode.TileEditor:
                 // Open the tile editor (fallback if called directly)
                 var tileView = new TileEditorView(_player);
                 WindowDirector?.Value.OpenWindow(tileView.Presenter);
                 break;

             default:
                 WindowDirector?.Value.OpenPopup(_player, "Error", "Unknown editor mode.", false);
                 break;
         }
     }

     private void HandleOpenTool()
     {
         // Read selection from bind
         int selected = Token().GetBindValue(View.ToolsSelected);
         LevelTool tool = (LevelTool)selected;

         switch (tool)
         {
             case LevelTool.TileEditor:
                 // Open the dedicated TileEditorView
                 var tileView = new TileEditorView(_player);
                 WindowDirector?.Value.OpenWindow(tileView.Presenter);
                 break;
             case LevelTool.PlcEditor:
                 // Open the existing PLC editor view
                 var plcView = new PlcEditorView(_player);
                 WindowDirector?.Value.OpenWindow(plcView.Presenter);
                 break;
             default:
                 WindowDirector?.Value.OpenPopup(_player, "Tool Error", "Unknown tool selection.", false);
                 break;
         }
     }

     public override void UpdateView()
     {
         // No dynamic binds for this simple toolbar yet.
     }

     public override void Close()
     {
         // Close the Nui window if still open
         try
         {
            try
            {
                _token.Close();
            }
            catch
            {
                // ignore
            }
         }
         catch
         {
             // ignore
         }
     }
 }
