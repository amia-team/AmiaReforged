using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.DreamcoinTool;

public sealed class PlayerDcSelfView : ScryView<PlayerDcSelfPresenter>
{
    private const float WindowWidth = 300f;
    private const float WindowHeight = 150f;

    public override PlayerDcSelfPresenter Presenter { get; protected set; }

    // Binds
    public readonly NuiBind<string> CurrentBalance = new("current_balance");
    public readonly NuiBind<string> BurnRewardInfo = new("burn_reward_info");

    // Buttons
    public const string BurnButtonId = "btn_burn_dc";

    public PlayerDcSelfView(NwPlayer player, DreamcoinService dreamcoinService)
    {
        Presenter = new PlayerDcSelfPresenter(this, player, dreamcoinService);
    }

    public override NuiLayout RootLayout()
    {
        return new NuiColumn
        {
            Children = new List<NuiElement>
            {
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel(CurrentBalance) { Width = WindowWidth - 20f }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel(BurnRewardInfo) { Width = WindowWidth - 20f }
                    }
                },
                new NuiSpacer(),
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiButton("Burn 1 DC for Gold & XP") { Id = BurnButtonId, Width = WindowWidth - 40f }
                    }
                }
            }
        };
    }

    public float GetWindowWidth() => WindowWidth;
    public float GetWindowHeight() => WindowHeight;
}
