using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Action = System.Action;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui;

/// <summary>
///     Presenter for the trait onboarding prompt popup.
///     </summary>
/// <remarks>
///     This presenter is intentionally thin: it routes NUI clicks to the callbacks supplied by the
///     caller and never queries trait repositories, decides eligibility, calculates budgets, writes
///     preference values, or opens <see cref="TraitSelectionView"/>. The suppression callback is
///     responsible for persisting the "don't show again" preference.
/// </remarks>
public sealed class TraitOnboardingPromptPresenter : ScryPresenter<TraitOnboardingPromptView>
{
    private readonly NwPlayer _player;
    private readonly Action _onYes;
    private readonly Action _onSuppressionRequested;

    private NuiWindowToken _token;
    private NuiWindow? _window;

    public TraitOnboardingPromptPresenter(
        NwPlayer player,
        TraitOnboardingPromptView view,
        Action onYes,
        Action onSuppressionRequested)
    {
        _player = player;
        _onYes = onYes;
        _onSuppressionRequested = onSuppressionRequested;
        View = view;
    }

    public override TraitOnboardingPromptView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), TraitOnboardingPromptView.Title)
        {
            Geometry = new NuiRect(400f, 300f, 380f, 200f),
            Resizable = false
        };
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click) return;

        switch (obj.ElementId)
        {
            case TraitOnboardingPromptView.YesButtonId:
                OnYes();
                break;

            case TraitOnboardingPromptView.NoButtonId:
                OnNo();
                break;
        }
    }

    public override void UpdateView()
    {
        // No updates needed; the checkbox defaults to false.
    }

    public override void Create()
    {
        InitBefore();
        _player.TryCreateNuiWindow(_window!, out _token);

        // The checkbox defaults to unchecked (false).
        Token().SetBindValue(View.SuppressReminder, false);
    }

    public override void Close()
    {
        _token.Close();
    }

    // ──────────────────── Actions ────────────────────

    /// <summary>
    ///     Handles the Yes control: closes the prompt and invokes only the Yes callback.
    ///     No preference value is written.
    /// </summary>
    private void OnYes()
    {
        Close();
        _onYes.Invoke();
    }

    /// <summary>
    ///     Handles the No control: reads the suppression checkbox.
    ///     When checked, closes the prompt and invokes only the suppression callback.
    ///     When unchecked, closes the prompt and invokes nothing (persists nothing).
    /// </summary>
    private void OnNo()
    {
        bool suppress = Token().GetBindValue(View.SuppressReminder);

        Close();

        if (suppress)
        {
            _onSuppressionRequested.Invoke();
        }
    }
}
