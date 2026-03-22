using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Nui;

/// <summary>
/// NUI view for the crafting progress bar window.
/// Layout: blank spacer at top (extensible for future in-between effects),
/// status label, progress bar, time remaining label, and a "Done" button that
/// appears only after crafting completes.
/// </summary>
public sealed class CraftingProgressView : ScryView<CraftingProgressPresenter>
{
    public const float WindowW = 400f;
    public const float WindowH = 300f;

    // --- Binds ---
    public readonly NuiBind<string> StatusText = new("cp_status");
    public readonly NuiBind<float> ProgressValue = new("cp_progress");
    public readonly NuiBind<string> TimeRemainingText = new("cp_time_remaining");
    public readonly NuiBind<bool> ShowDoneButton = new("cp_show_done");

    public CraftingProgressView(
        NwPlayer player,
        Recipe recipe,
        AggregatedCraftingModifiers modifiers,
        CharacterId characterId,
        List<int> selectedQualities)
    {
        Presenter = new CraftingProgressPresenter(this, player, recipe, modifiers, characterId, selectedQualities);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override CraftingProgressPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        return new NuiColumn
        {
            Children =
            [
                // Blank space at top — extensible area for future in-between effects
                new NuiSpacer { Height = 100f },

                // Status label (e.g. "Crafting Iron Sword...")
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

                // Time remaining label
                new NuiRow
                {
                    Height = 24f,
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiLabel(TimeRemainingText)
                        {
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(160, 140, 100)
                        },
                        new NuiSpacer()
                    ]
                },

                new NuiSpacer(),

                // Done button — only visible after crafting completes
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiButton("Done")
                        {
                            Id = "btn_done",
                            Width = 100f,
                            Height = 32f,
                            Visible = ShowDoneButton
                        },
                        new NuiSpacer()
                    ]
                }
            ]
        };
    }
}
