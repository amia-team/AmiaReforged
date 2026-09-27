using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
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
    private readonly TraitSelectionWindowService _selectionWindowService;

    public TraitSelectionCommand(TraitSelectionWindowService selectionWindowService)
    {
        _selectionWindowService = selectionWindowService;
    }

    public string Command => "./traits";
    public string Description => "Opens the trait selection window";
    public string AllowedRoles => "Player";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (caller.IsDM) return Task.CompletedTask;

        _selectionWindowService.Open(caller);
        return Task.CompletedTask;
    }
}
