using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public sealed class MythalLedgerView : ScryView<MythalLedgerPresenter>
{
    public NuiBind<string> DivineMythalCount = new(key: "divine_mythals");
    public NuiBind<string> FlawlessMythalCount = new(key: "flawless_mythals");
    public NuiBind<string> GreaterMythalCount = new(key: "greater_mythals");
    public NuiBind<string> IntermediateMythalCount = new(key: "intermediate_mythals");
    public NuiBind<string> LesserMythalCount = new(key: "lesser_mythals");

    public NuiBind<string> MinorMythalCount = new(key: "minor_mythals");
    public MythalForgePresenter Parent;
    public NuiBind<string> PerfectMythalCount = new(key: "perfect_mythals");


    public MythalLedgerView(MythalForgePresenter parent, NwPlayer player)
    {
        Parent = parent;
        Presenter = new MythalLedgerPresenter(parent, player, this);
    }

    public override MythalLedgerPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout() =>
        new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Minor:"),
                        new NuiLabel(MinorMythalCount)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Lesser:"),
                        new NuiLabel(LesserMythalCount)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Intermediate:"),
                        new NuiLabel(IntermediateMythalCount)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Greater:"),
                        new NuiLabel(GreaterMythalCount)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Flawless:"),
                        new NuiLabel(FlawlessMythalCount)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Perfect:"),
                        new NuiLabel(PerfectMythalCount)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Divine:"),
                        new NuiLabel(DivineMythalCount)
                    }
                }
            }
        };
}