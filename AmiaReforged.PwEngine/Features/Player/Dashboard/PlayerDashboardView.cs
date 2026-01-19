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
                // Placeholder content
                new NuiColumn
                {
                    Children =
                    [
                        RestButton = new NuiButton("Rest")
                        {
                            Id = "btn_rest",
                            Width = 100f,
                            Height = 25f
                        },
                        new NuiButton("Pray")
                        {
                            Id = "btn_pray",
                            Width = 100f,
                            Height = 25f
                        },
                        new NuiButton("Hide")
                        {
                            Id = "btn_hide",
                            Width = 100f,
                            Height = 25f
                        },
                        new NuiButton("Emotes")
                        {
                            Id = "btn_emotes",
                            Width = 100f,
                            Height = 25f
                        },
                        new NuiButton("Player Tools")
                        {
                            Id = "btn_player_tools",
                            Width = 100f,
                            Height = 25f
                        },
                        new NuiButton("Utilities")
                        {
                            Id = "btn_utilities",
                            Width = 100f,
                            Height = 25f
                        },
                        new NuiRow
                        {
                            Children =
                            {
                                new NuiSpacer { Width = 30f },
                                new NuiButton("X")
                                {
                                    Id = "btn_close",
                                    Width = 20f,
                                    Height = 25f,
                                    Tooltip = "Close Dashboard"
                                }
                            }
                        }
                    ]
                },
            ]
        };
        return root;
    }
}
