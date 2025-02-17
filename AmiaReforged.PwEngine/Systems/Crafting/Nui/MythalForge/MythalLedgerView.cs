using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public sealed class MythalLedgerView : ScryView<MythalLedgerPresenter>
{
    public MythalForgePresenter Parent;
    public sealed override MythalLedgerPresenter Presenter { get; protected set; }

    public NuiBind<string> MinorMythalCount = new("minor_mythals");
    public NuiBind<string> LesserMythalCount = new("lesser_mythals");
    public NuiBind<string> IntermediateMythalCount = new("intermediate_mythals");
    public NuiBind<string> GreaterMythalCount = new("greater_mythals");
    public NuiBind<string> FlawlessMythalCount = new("flawless_mythals");
    public NuiBind<string> PerfectMythalCount = new("perfect_mythals");
    public NuiBind<string> DivineMythalCount = new("divine_mythals");


    public MythalLedgerView(MythalForgePresenter parent, NwPlayer player)
    {
        Parent = parent;
        Presenter = new MythalLedgerPresenter(parent, player, this);
    }

    public override NuiLayout RootLayout()
    {
        return new NuiColumn()
        {
            Children =
            {
                new NuiRow()
                {
                    Children =
                    {
                        new NuiLabel("Minor:"),
                        new NuiLabel(MinorMythalCount)
                    }
                },
                new NuiRow()
                {
                    Children =
                    {
                        new NuiLabel("Lesser:"),
                        new NuiLabel(LesserMythalCount)
                    }
                },
                new NuiRow()
                {
                    Children =
                    {
                        new NuiLabel("Intermediate:"),
                        new NuiLabel(IntermediateMythalCount)
                    }
                },
                new NuiRow()
                {
                    Children =
                    {
                        new NuiLabel("Greater:"),
                        new NuiLabel(GreaterMythalCount)
                    }
                },
                new NuiRow()
                {
                    Children =
                    {
                        new NuiLabel("Flawless:"),
                        new NuiLabel(FlawlessMythalCount)
                    }
                },
                new NuiRow()
                {
                    Children =
                    {
                        new NuiLabel("Perfect:"),
                        new NuiLabel(PerfectMythalCount)
                    }
                },
                new NuiRow()
                {
                    Children =
                    {
                        new NuiLabel("Divine:"),
                        new NuiLabel(DivineMythalCount)
                    }
                }
            }
        };
    }
}