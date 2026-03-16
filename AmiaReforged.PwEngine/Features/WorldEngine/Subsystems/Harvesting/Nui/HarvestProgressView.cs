using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Nui;

/// <summary>
/// Minimal NUI view for harvesting progress. Displays only a progress bar —
/// no status text, no time remaining, no done button.
/// </summary>
public sealed class HarvestProgressView : ScryView<HarvestProgressPresenter>
{
    public const float WindowW = 300f;
    public const float WindowH = 80f;

    public readonly NuiBind<float> ProgressValue = new("hp_progress");

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
                new NuiSpacer { Height = 15f },

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

                new NuiSpacer { Height = 15f }
            ]
        };
    }
}
