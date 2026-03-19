using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Nui;

/// <summary>
/// NUI view for the interaction progress bar popup.
/// Layout: status label, progress bar, rounds-remaining label.
/// Auto-closes on completion, failure, or cancellation — no Done button.
/// </summary>
public sealed class InteractionProgressView : ScryView<InteractionProgressPresenter>
{
    public const float WindowW = 400f;
    public const float WindowH = 180f;

    // --- Binds ---
    public readonly NuiBind<string> StatusText = new("ip_status");
    public readonly NuiBind<float> ProgressValue = new("ip_progress");
    public readonly NuiBind<string> RoundsRemainingText = new("ip_rounds_remaining");

    public InteractionProgressView(
        NwPlayer player,
        string interactionTag,
        CharacterId characterId,
        Guid targetId,
        string? areaResRef)
    {
        Presenter = new InteractionProgressPresenter(
            this, player, interactionTag, characterId, targetId, areaResRef);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override InteractionProgressPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        return new NuiColumn
        {
            Children =
            [
                new NuiSpacer { Height = 16f },

                // Status label (e.g. "Prospecting..." / "Complete!" / "Failed")
                new NuiRow
                {
                    Height = 30f,
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiLabel(StatusText)
                        {
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiSpacer()
                    ]
                },

                new NuiSpacer { Height = 8f },

                // Progress bar
                new NuiRow
                {
                    Height = 30f,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiProgress(ProgressValue)
                        {
                            Height = 24f
                        },
                        new NuiSpacer { Width = 20f }
                    ]
                },

                new NuiSpacer { Height = 4f },

                // Rounds remaining label
                new NuiRow
                {
                    Height = 24f,
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiLabel(RoundsRemainingText)
                        {
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(160, 140, 100)
                        },
                        new NuiSpacer()
                    ]
                },

                new NuiSpacer()
            ]
        };
    }
}
