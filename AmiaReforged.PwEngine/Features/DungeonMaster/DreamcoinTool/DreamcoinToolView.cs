using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.DreamcoinTool;

public sealed class DreamcoinToolView : ScryView<DreamcoinToolPresenter>
{
    private const float WindowWidth = 500f;
    private const float WindowHeight = 200f;

    public override DreamcoinToolPresenter Presenter { get; protected set; }

    // Binds
    public readonly NuiBind<string> TargetName = new("target_name");
    public readonly NuiBind<string> CurrentBalance = new("current_balance");
    public readonly NuiBind<string> AddAmount = new("add_amount");
    public readonly NuiBind<string> TakeAmount = new("take_amount");

    // Buttons
    public const string AddButtonId = "btn_add_dc";
    public const string AddPartyButtonId = "btn_add_dc_party";
    public const string AddNearbyButtonId = "btn_add_dc_nearby";
    public const string TakeButtonId = "btn_take_dc";

    public DreamcoinToolView(NwPlayer dmPlayer, NwPlayer targetPlayer, DreamcoinService dreamcoinService)
    {
        Presenter = new DreamcoinToolPresenter(this, dmPlayer, targetPlayer, dreamcoinService);
    }

    public override NuiLayout RootLayout()
    {
        return new NuiColumn
        {
            Children = new List<NuiElement>
            {
                // Target info
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
                        new NuiLabel(CurrentBalance) { Width = WindowWidth - 20f }
                    }
                },
                new NuiSpacer(),

                // Add DCs row
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel("Add DCs:") { Width = 80f },
                        new NuiTextEdit("Amount", AddAmount, 10, false) { Width = 80f },
                        new NuiButton("Confirm") { Id = AddButtonId, Width = 70f },
                        new NuiButton("Party") { Id = AddPartyButtonId, Width = 60f, Tooltip = "Give to party members in same area" },
                        new NuiButton("Nearby") { Id = AddNearbyButtonId, Width = 60f, Tooltip = "Give to players within 5 meters" }
                    }
                },

                // Take DCs row
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel("Take DCs:") { Width = 80f },
                        new NuiTextEdit("Amount", TakeAmount, 10, false) { Width = 100f },
                        new NuiButton("Confirm") { Id = TakeButtonId, Width = 80f }
                    }
                }
            }
        };
    }

    public float GetWindowWidth() => WindowWidth;
    public float GetWindowHeight() => WindowHeight;
}
