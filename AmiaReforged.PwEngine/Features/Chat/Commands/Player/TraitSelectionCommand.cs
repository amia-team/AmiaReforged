using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Player;

/// <summary>
///     Opens the trait selection window via <c>./traits</c>.
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class TraitSelectionCommand : IChatCommand
{
    private readonly WindowDirector _director;

    public TraitSelectionCommand(WindowDirector director)
    {
        _director = director;
    }

    public string Command => "./traits";
    public string Description => "Opens the trait selection window";
    public string AllowedRoles => "Player";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (caller.IsDM) return Task.CompletedTask;

        if (_director.IsWindowOpen(caller, typeof(TraitSelectionPresenter)))
            return Task.CompletedTask;

        TraitSelectionView view = new(caller);
        IScryPresenter presenter = view.Presenter;

        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector != null)
            injector.Inject(presenter);

        _director.OpenWindow(presenter);
        return Task.CompletedTask;
    }
}
