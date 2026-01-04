using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.DreamcoinTool;

public sealed class PlayerDcSelfView : ScryView<PlayerDcSelfPresenter>
{
    private const float WindowWidth = 300f;
    private const float WindowHeight = 280f;

    public override PlayerDcSelfPresenter Presenter { get; protected set; }

    // Binds
    public readonly NuiBind<string> CurrentBalance = new("current_balance");
    public readonly NuiBind<string> NextDcTimer = new("next_dc_timer");
    public readonly NuiBind<string> BurnRewardInfo = new("burn_reward_info");
    public readonly NuiBind<string> BurnGoldOnlyInfo = new("burn_gold_only_info");
    public readonly NuiBind<bool> CanBurnForXp = new("can_burn_xp");

    // Buttons
    public const string BurnButtonId = "btn_burn_dc";
    public const string BurnGoldOnlyButtonId = "btn_burn_dc_gold";

    public PlayerDcSelfView(NwPlayer player, DreamcoinService dreamcoinService, DcPlaytimeService playtimeService)
    {
        Presenter = new PlayerDcSelfPresenter(this, player, dreamcoinService, playtimeService);
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
                        new NuiLabel(NextDcTimer) { Width = WindowWidth - 20f }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel(BurnRewardInfo) { Width = WindowWidth - 20f }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiButton("Burn 1 DC for Gold & XP") { Id = BurnButtonId, Width = WindowWidth - 40f, Enabled = CanBurnForXp }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel(BurnGoldOnlyInfo) { Width = WindowWidth - 20f }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiButton("Burn 1 DC for Gold Only") { Id = BurnGoldOnlyButtonId, Width = WindowWidth - 40f }
                    }
                }
            }
        };
    }

    public float GetWindowWidth() => WindowWidth;
    public float GetWindowHeight() => WindowHeight;
}
