using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui;

public sealed class PlayerToolsWindowView : ScryView<PlayerToolsWindowPresenter>
{
    // Value binds.
    public readonly NuiBind<string> Search = new(key: "search_val");
    public readonly NuiBind<int> WindowCount = new(key: "window_count");
    public readonly NuiBind<string> WindowNames = new(key: "win_names");
    public NuiButtonImage OpenWindowButton = null!;

    // Buttons.
    public NuiButtonImage SearchButton = null!;

    public PlayerToolsWindowView(NwPlayer player)
    {
        Presenter = new PlayerToolsWindowPresenter(this, player);
    }

    public override PlayerToolsWindowPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> rowTemplate =
        [
            new(new NuiButtonImage(resRef: "dm_goto")
            {
                Id = "btn_openwin",
                Aspect = 1f,
                Tooltip = "Open Window"
            }.Assign(out OpenWindowButton))
            {
                VariableSize = false,
                Width = 35f
            },

            new(new NuiLabel(WindowNames)
            {
                VerticalAlign = NuiVAlign.Middle
            })
        ];

        NuiColumn root = new()
        {
            Children =
            [
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiTextEdit(label: "Search for tools...", Search, 255, false),
                        new NuiButtonImage(resRef: "isk_search")
                        {
                            Id = "btn_search",
                            Aspect = 1f
                        }.Assign(out SearchButton)
                    ]
                },

                new NuiList(rowTemplate, WindowCount)
                {
                    RowHeight = 35f
                }
            ]
        };

        return root;
    }
}