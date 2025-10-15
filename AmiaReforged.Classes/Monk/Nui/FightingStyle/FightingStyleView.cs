using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using static AmiaReforged.Classes.Monk.Nui.FightingStyle.FightingStyleNuiElements;

namespace AmiaReforged.Classes.Monk.Nui.FightingStyle;

public sealed class FightingStyleView : ScryView<FightingStylePresenter>
{
    public NuiButton KnockdownStyleButton = null!;
    public NuiButton DisarmStyleButton = null!;
    public NuiButton RangedStyleButton = null!;
    public FightingStyleView(NwPlayer player)
    {
        Presenter = new FightingStylePresenter(this, player);
    }
    public override FightingStylePresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout() =>
        new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiButton(KnockdownStyleName)
                        {
                            Tooltip = "Click to confirm",
                            Id = "knockdown_button"
                        }.Assign(out KnockdownStyleButton),
                        new NuiText(KnockdownStyleDescription)
                        {
                            Border = false,
                            Scrollbars = NuiScrollbars.None
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiButton(DisarmStyleName)
                        {
                            Tooltip = "Click to confirm",
                            Id = "disarm_button"
                        }.Assign(out DisarmStyleButton),
                        new NuiText(DisarmStyleDescription)
                        {
                            Border = false,
                            Scrollbars = NuiScrollbars.None
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiButton(RangedStyleName)
                        {
                            Tooltip = "Click to confirm",
                            Id = "ranged_button"
                        }.Assign(out RangedStyleButton),
                        new NuiText(RangedStyleDescription)
                        {
                            Border = false,
                            Scrollbars = NuiScrollbars.None
                        }
                    }
                }
            }

        };
}
