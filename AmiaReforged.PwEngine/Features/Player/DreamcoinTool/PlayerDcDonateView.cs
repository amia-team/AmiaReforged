using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.DreamcoinTool;

public sealed class PlayerDcDonateView : ScryView<PlayerDcDonatePresenter>
{
    private const float WindowWidth = 320f;
    private const float WindowHeight = 150f;

    public override PlayerDcDonatePresenter Presenter { get; protected set; }

    // Binds
    public readonly NuiBind<string> TargetName = new("target_name");
    public readonly NuiBind<string> DonateStatus = new("donate_status");

    // Buttons
    public const string DonateButtonId = "btn_donate_dc";
    public const string RecommendButtonId = "btn_recommend";

    public PlayerDcDonateView(NwPlayer player, NwPlayer targetPlayer, DreamcoinService dreamcoinService)
    {
        Presenter = new PlayerDcDonatePresenter(this, player, targetPlayer, dreamcoinService);
    }

    public override NuiLayout RootLayout()
    {
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowWidth, WindowHeight))]
        };

        return new NuiColumn
        {
            Children = new List<NuiElement>
            {
                bgLayer,
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel(TargetName) { Width = WindowWidth - 20f }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel(DonateStatus) { Width = WindowWidth - 20f }
                    }
                },
                new NuiSpacer(),
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiButton("Donate 1 DC") { Id = DonateButtonId, Width = 140f },
                        new NuiButton("Recommend") { Id = RecommendButtonId, Width = 140f, Tooltip = "Recommend for good RP" }
                    }
                }
            }
        };
    }

    public float GetWindowWidth() => WindowWidth;
    public float GetWindowHeight() => WindowHeight;
}
