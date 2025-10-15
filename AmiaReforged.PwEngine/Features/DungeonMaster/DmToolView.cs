using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster;

public sealed class DmToolView : ScryView<DmToolPresenter>
{

    public readonly NuiBind<string> Search = new(key: "search_val");
    public readonly NuiBind<int> WindowCount = new(key: "window_count");
    public readonly NuiBind<string> WindowNames = new(key: "win_names");
    public NuiButtonImage OpenWindowButton = null!;
    public NuiButtonImage SearchButton = null!;

    public override DmToolPresenter Presenter { get; protected set; }

    public DmToolView(NwPlayer player)
    {
        Presenter = new DmToolPresenter(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

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
