using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

/// <summary>
///     Shared entry point for opening the trait-selection window. Centralizes the existing
///     <c>/traits</c> behavior so both the command and the onboarding Yes path open exactly one
///     window.
/// </summary>
/// <remarks>
///     This is the service that Story 002 provides; it is referenced by
///     <see cref="TraitOnboardingService" />. The <see cref="TraitSelectionView" /> constructor
///     performs presenter injection, so this service only guards against duplicates and opens the
///     window through the <see cref="WindowDirector" />.
/// </remarks>
[ServiceBinding(typeof(TraitSelectionWindowService))]
public class TraitSelectionWindowService
{
    private readonly WindowDirector _windowDirector;

    public TraitSelectionWindowService(WindowDirector windowDirector)
    {
        _windowDirector = windowDirector;
    }

    /// <summary>
    ///     Opens the trait-selection window for <paramref name="player" /> unless it is already open.
    /// </summary>
    public void Open(NwPlayer player)
    {
        if (player is null) return;

        if (_windowDirector.IsWindowOpen(player, typeof(TraitSelectionPresenter))) return;

        TraitSelectionView view = new(player);
        _windowDirector.OpenWindow(view.Presenter);
    }
}
