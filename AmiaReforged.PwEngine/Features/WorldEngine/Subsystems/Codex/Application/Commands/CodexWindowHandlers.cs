using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Commands;

/// <summary>
/// Handles <see cref="OpenCodexCommand"/> — creates and opens the Codex NUI window,
/// enforcing at most one Codex window per player.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<OpenCodexCommand>))]
public sealed class OpenCodexHandler : ICommandHandler<OpenCodexCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly WindowDirector _windowDirector;

    public OpenCodexHandler(WindowDirector windowDirector)
    {
        _windowDirector = windowDirector;
    }

    public Task<CommandResult> HandleAsync(OpenCodexCommand command, CancellationToken cancellationToken = default)
    {
        if (_windowDirector.IsWindowOpen(command.Player, typeof(PlayerCodexPresenter)))
        {
            return Task.FromResult(CommandResult.Fail("Your codex is already open."));
        }

        try
        {
            PlayerCodexView view = new(command.Player);
            _windowDirector.OpenWindow(view.Presenter);
            return Task.FromResult(CommandResult.Ok());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open Codex window for {Player}", command.Player.PlayerName);
            return Task.FromResult(CommandResult.Fail("Unable to open the codex window."));
        }
    }
}

/// <summary>
/// Handles <see cref="CloseCodexCommand"/> — closes the player's Codex window if open.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<CloseCodexCommand>))]
public sealed class CloseCodexHandler : ICommandHandler<CloseCodexCommand>
{
    private readonly WindowDirector _windowDirector;

    public CloseCodexHandler(WindowDirector windowDirector)
    {
        _windowDirector = windowDirector;
    }

    public Task<CommandResult> HandleAsync(CloseCodexCommand command, CancellationToken cancellationToken = default)
    {
        if (!_windowDirector.IsWindowOpen(command.Player, typeof(PlayerCodexPresenter)))
        {
            return Task.FromResult(CommandResult.Fail("No codex window is open."));
        }

        _windowDirector.CloseWindow(command.Player, typeof(PlayerCodexPresenter));
        return Task.FromResult(CommandResult.Ok());
    }
}
