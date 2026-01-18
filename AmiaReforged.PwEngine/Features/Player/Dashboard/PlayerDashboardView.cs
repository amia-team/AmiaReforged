using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard;

public sealed class PlayerDashboardView : ScryView<PlayerDashboardPresenter>
{
    public override PlayerDashboardPresenter Presenter { get; protected set; }

    public NuiButton RestButton { get; private set; } = null!;

    public PlayerDashboardView(NwPlayer player)
    {
        Presenter = new PlayerDashboardPresenter(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children =
            [
                // Background image
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 450f, 300f))]
                },

                // Placeholder content
                new NuiGroup
                {
                    Element = new NuiColumn
                    {
                        Children =
                        [
                            new NuiSpacer { Height = 100f },
                            new NuiLabel("Player Dashboard")
                            {
                                Width = 300f,
                                HorizontalAlign = NuiHAlign.Center,
                                VerticalAlign = NuiVAlign.Middle
                            },
                            new NuiSpacer { Height = 20f },
                            new NuiLabel("(Under Construction)")
                            {
                                Width = 300f,
                                HorizontalAlign = NuiHAlign.Center,
                                VerticalAlign = NuiVAlign.Middle
                            },
                            new NuiSpacer { Height = 20f },
                            RestButton = new NuiButton("Rest")
                            {
                                Id = "btn_rest",
                                Width = 100f,
                                Height = 35f
                            },
                            new NuiButton("Pray")
                            {
                                Id = "btn_pray",
                                Width = 100f,
                                Height = 35f
                            },
                            new NuiButton("Hide")
                            {
                                Id = "btn_hide",
                                Width = 100f,
                                Height = 35f
                            },
                            new NuiButton("Emotes")
                            {
                                Id = "btn_emotes",
                                Width = 100f,
                                Height = 35f
                            },
                            new NuiButton("Player Tools")
                            {
                                Id = "btn_player_tools",
                                Width = 100f,
                                Height = 35f
                            },
                            new NuiButton("Utilities")
                            {
                                Id = "btn_utilities",
                                Width = 100f,
                                Height = 35f
                            },
                            new NuiSpacer { Height = 20f }
                        ]
                    },
                    Border = false,
                    Width = 380f,
                    Height = 450f
                }
            ]
        };

        return root;
    }
}
