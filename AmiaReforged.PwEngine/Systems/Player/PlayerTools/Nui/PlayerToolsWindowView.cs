using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui;

public sealed class PlayerToolsWindowView : ScryView<PlayerToolsWindowPresenter>
{
    
    public override PlayerToolsWindowPresenter Presenter { get; protected set; }
    public PlayerToolsWindowView(NwPlayer player)
    {
        Presenter = new PlayerToolsWindowPresenter(this, player);
    }
    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> rowTemplate = new()
        {
            new NuiListTemplateCell(new NuiButtonImage("dm_goto")
            {
                Id = "btn_openwin",
                Aspect = 1f,
                Tooltip = "Open Window",
            }.Assign(out OpenWindowButton))
            {
                VariableSize = false,
                Width = 35f,
            },
            new NuiListTemplateCell(new NuiLabel(WindowNames)
            {
                VerticalAlign = NuiVAlign.Middle,
            }),
        };

        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiRow
                {
                    Height = 40f,
                    Children = new List<NuiElement>
                    {
                        new NuiTextEdit("Search for tools...", Search, 255, false),
                        new NuiButtonImage("isk_search")
                        {
                            Id = "btn_search",
                            Aspect = 1f,
                        }.Assign(out SearchButton),
                    },
                },
                new NuiList(rowTemplate, WindowCount)
                {
                    RowHeight = 35f,
                },
            },
        };

        return root;
    }

    // Value binds.
    public readonly NuiBind<string> Search = new("search_val");
    public readonly NuiBind<string> WindowNames = new NuiBind<string>("win_names");
    public readonly NuiBind<int> WindowCount = new NuiBind<int>("window_count");
    
    // Buttons.
    public NuiButtonImage SearchButton;
    public NuiButtonImage OpenWindowButton;
}