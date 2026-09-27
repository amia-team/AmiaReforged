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
        // DMs are not permitted to open the player trait-selection window.
        if (player is null || player.IsDM) return;

        // Never open a second trait-selection window for the same player.
        if (_windowDirector.IsWindowOpen(player, typeof(TraitSelectionPresenter))) return;

        // The view constructs the presenter and injects its dependencies via InjectionService.
        TraitSelectionView view = new(player);
        _windowDirector.OpenWindow(view.Presenter);
    }
}
