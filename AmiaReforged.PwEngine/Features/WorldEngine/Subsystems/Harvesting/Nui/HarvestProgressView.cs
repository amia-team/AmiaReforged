using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Nui;

/// <summary>
/// Minimal NUI view for harvesting progress. Displays a status label
/// and a progress bar inside a transparent, borderless window.
/// </summary>
public sealed class HarvestProgressView : ScryView<HarvestProgressPresenter>
{
    public const float WindowW = 400f;
    public const float WindowH = 120f;

    public readonly NuiBind<float> ProgressValue = new("hp_progress");
    public readonly NuiBind<string> StatusText = new("hp_status");

    public HarvestProgressView(NwPlayer player, string title)
    {
        Presenter = new HarvestProgressPresenter(this, player, title);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override HarvestProgressPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        return new NuiColumn
        {
            Children =
            [
                new NuiSpacer { Height = 10f },

                // Action label (e.g. "Chopping Oak Tree")
                new NuiRow
                {
                    Height = 26f,
                    Children =
                    [
                        new NuiLabel(StatusText)
                        {
                            Width = WindowW - 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    ]
                },

                new NuiSpacer { Height = 8f },

                // Progress bar
                new NuiRow
                {
                    Height = 30f,
                    Children =
                    [
                        new NuiSpacer { Width = 15f },
                        new NuiProgress(ProgressValue)
                        {
                            Height = 24f
                        },
                        new NuiSpacer { Width = 15f }
                    ]
                },

                new NuiSpacer { Height = 10f }
            ]
        };
    }
}
