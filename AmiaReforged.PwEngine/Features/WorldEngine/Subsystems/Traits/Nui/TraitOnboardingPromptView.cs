using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Action = System.Action;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui;

/// <summary>
///     Small, modal-like Scry popup that asks the player whether they want to select traits.
///     </summary>
/// <remarks>
///     This view only renders the prompt (title, body, "Don't show this reminder again" checkbox,
///     and Yes/No controls). It contains no trait-selection business logic, no repository access,
///     and no persistence. All outcomes are handed to the presenter as callbacks.
/// </remarks>
public sealed class TraitOnboardingPromptView : ScryView<TraitOnboardingPromptPresenter>
{
    /// <summary>
    ///     Exact title shown in the window header.
    /// </summary>
    public const string Title = "Select Traits";

    /// <summary>
    ///     Exact body text shown in the popup.
    /// </summary>
    public const string Body =
        "You have not selected traits for this character. Would you like to select them now?";

    /// <summary>
    ///     Exact label for the suppression checkbox.
    /// </summary>
    public const string CheckboxLabel = "Don't show this reminder again";

    /// <summary>
    ///     NUI element id for the Yes button.
    /// </summary>
    public const string YesButtonId = "onboarding_yes_button";

    /// <summary>
    ///     NUI element id for the No button.
    /// </summary>
    public const string NoButtonId = "onboarding_no_button";

    /// <summary>
    ///     Bind key backing the "Don't show this reminder again" checkbox.
    /// </summary>
    public const string CheckboxKey = "trait_onboarding_suppress";

    public readonly NuiBind<bool> SuppressReminder = new(CheckboxKey);

    public TraitOnboardingPromptView(NwPlayer player, Action onYes, Action onSuppressionRequested)
    {
        Presenter = new TraitOnboardingPromptPresenter(player, this, onYes, onSuppressionRequested);
    }

    public override TraitOnboardingPromptPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        NuiColumn popupLayout = new()
        {
            Children =
            {
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 380f, 200f))]
                },
                new NuiGroup
                {
                    Element = new NuiText(Body)
                    {
                        Scrollbars = NuiScrollbars.None
                    },
                    Border = true,
                    Width = 340,
                    Height = 90
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 20f },
                        new NuiCheck(CheckboxLabel, SuppressReminder)
                        {
                            Width = 260f,
                            Tooltip = "Dismiss this prompt and do not show it again"
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer(),
                        new NuiButton(label: "Yes")
                        {
                            Id = YesButtonId,
                            Width = 90f,
                            Height = 60f,
                            Encouraged = true
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiButton(label: "No")
                        {
                            Id = NoButtonId,
                            Width = 90f,
                            Height = 60f
                        },
                        new NuiSpacer()
                    }
                }
            }
        };
        return popupLayout;
    }
}
