using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.DungeonMaster.PlcEdit;
using AmiaReforged.PwEngine.Features.DungeonMaster.LevelEdit.AreaEdit;
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

    public string Title => "Level Editor";
    public bool ListInDmTools => true;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    // Buttons exposed for the presenter to wire up
    public NuiButton InstanceSelectionButton = null!;
    public NuiButton AreaSettingsButton = null!;
    // Tools dropdown and open button
    public readonly NuiBind<int> ToolsSelected = new("tools_selected");
    public NuiCombo ToolsCombo = null!;
    public NuiButton OpenToolButton = null!;
    public NuiButton HelpButton = null!;

    // Bind to display current area name
    public readonly NuiBind<string> CurrentAreaName = new("current_area_name");

    public LevelEditView(NwPlayer player)
    {
        Presenter = new LevelEditPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        // Simple horizontal toolbar showing current area and providing access to tools

        // Create elements explicitly to avoid ambiguous extension method resolution for Assign()
        NuiButton instanceSelectionButton = new NuiButton("Instances") { Id = "btn_instance_selection", Height = 30f };
        InstanceSelectionButton = instanceSelectionButton;

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
            Element = new NuiColumn
            {
                Children =
                [
                    // Current area display
                    new NuiRow
                    {
                        Height = 25f,
                        Children =
                        [
                            new NuiLabel("Area: ") { Width = 50f, VerticalAlign = NuiVAlign.Middle },
                            new NuiLabel(CurrentAreaName) { VerticalAlign = NuiVAlign.Middle }
                        ]
                    },
                    // Main toolbar
                    new NuiRow
                    {
                        Height = 80f,
                        Children =
                        [
                            instanceSelectionButton,

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
    private NwArea? _currentArea;

    [Inject] private Lazy<WindowDirector>? WindowDirector { get; init; }
    [Inject] private Lazy<LevelEditorService>? LevelEditorService { get; init; }

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
            Geometry = new NuiRect(0f, 100f, 780f, 200f),
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

        // Track current area and set initial display
        _currentArea = _player.LoginCreature?.Area;
        UpdateCurrentAreaDisplay();

        // Subscribe to area exit event to detect when DM leaves the area
        if (_currentArea is not null)
        {
            _currentArea.OnExit += OnAreaExit;
        }

    }

    private void OnAreaExit(AreaEvents.OnExit obj)
    {
        // Check if it's our player leaving
        if (obj.ExitingObject != _player.LoginCreature) return;

        // Player left the area - close this window instance
        // The session persists, so unsaved work is safe
        Close();
    }

    private void UpdateCurrentAreaDisplay()
    {
        if (_currentArea is not null)
        {
            Token().SetBindValue(View.CurrentAreaName, $"{_currentArea.Name} ({_currentArea.ResRef})");
            // Defaults to the first entry to avoid null pointers in tool selection
            Token().SetBindValue(View.ToolsSelected, 0);
        }
        else
        {
            Token().SetBindValue(View.CurrentAreaName, "No Area");
        }
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click) return;

        switch (obj.ElementId)
        {
            case var id when id == View.InstanceSelectionButton.Id:
                // Open saved instances manager for current area
                var instancesView = new SavedInstancesView(_player);
                WindowDirector?.Value.OpenWindow(instancesView.Presenter);
                break;
            case var id when id == View.AreaSettingsButton.Id:
                OpenAreaSettings();
                break;
            case var id when id == View.OpenToolButton.Id:
                HandleOpenTool();
                break;
             case var id when id == View.HelpButton.Id:
                 WindowDirector?.Value.OpenPopup(_player, "Level Editor Help", "This toolbar works with your current area. Use Instances to manage saved area instances, Area Settings to modify fog/music, and Tools to access editors.", false);
                 break;
         }
     }

    private void OpenInstanceManager()
    {
        if (_currentArea is null)
        {
            return;
        }

        // Open the AreaEditorView which has the instance management UI
        AreaEditorView areaEditorView = new AreaEditorView(_player);
        WindowDirector?.Value.OpenWindow(areaEditorView.Presenter);
    }

    private void OpenAreaSettings()
    {
        if (_currentArea is null)
        {
            return;
        }

        // Open the dedicated Area Settings view
        AreaSettingsView settingsView = new AreaSettingsView(_player);
        WindowDirector?.Value.OpenWindow(settingsView.Presenter);
    }

     private void HandleOpenTool()
     {
         if (_currentArea is null)
         {
             return;
         }

         // Read selection from bind
         int selected = Token().GetBindValue(View.ToolsSelected);

         LevelTool tool = (LevelTool)selected;


         switch (tool)
         {
             case LevelTool.TileEditor:
                 // Open the dedicated TileEditorView
                 TileEditorView tileView = new TileEditorView(_player);
                 WindowDirector?.Value.OpenWindow(tileView.Presenter);
                 break;
             case LevelTool.PlcEditor:
                 // Open the existing PLC editor view
                 PlcEditorView plcView = new PlcEditorView(_player);
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
         // Unsubscribe from area exit event
         if (_currentArea is not null)
         {
             _currentArea.OnExit -= OnAreaExit;
         }

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
