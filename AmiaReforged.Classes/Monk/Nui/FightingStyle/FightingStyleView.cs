using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using static AmiaReforged.Classes.Monk.Nui.FightingStyle.FightingStyleNuiElements;

namespace AmiaReforged.Classes.Monk.Nui.FightingStyle;

public sealed class FightingStyleView : ScryView<FightingStylePresenter>
{
    public NuiButton KnockdownStyleButton = null!;
    public NuiButton DisarmStyleButton = null!;
    public NuiButton RangedStyleButton = null!;
    public NuiBind<string> ChosenStyle = new(key: "chosen_style");
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
