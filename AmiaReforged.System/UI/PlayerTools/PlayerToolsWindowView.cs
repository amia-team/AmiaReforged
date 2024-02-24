using AmiaReforged.Core.UserInterface;
using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools;

public sealed class PlayerToolsWindowView : WindowView<PlayerToolsWindowView>
{
    public override string Id => "playertools.mainwindow";
    public override string Title => "Player Tools";

    public override bool ListInPlayerTools => false;

    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<PlayerToolsWindowController>(player);
    }

    public override NuiWindow? WindowTemplate { get; }

    // Value binds.
    public readonly NuiBind<string> Search = new("search_val");
    public readonly NuiBind<string> WindowNames = new NuiBind<string>("win_names");
    public readonly NuiBind<int> WindowCount = new NuiBind<int>("window_count");


    // Buttons.
    public readonly NuiButtonImage SearchButton;
    public readonly NuiButtonImage OpenWindowButton;

    public PlayerToolsWindowView()
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

        WindowTemplate = new NuiWindow(root, Title)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f),
        };
    }
}