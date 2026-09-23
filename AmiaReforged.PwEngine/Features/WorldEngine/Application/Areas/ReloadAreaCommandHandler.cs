using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Areas;

/// <summary>
/// Handles <see cref="ReloadAreaCommand"/> by delegating the live NWN work to
/// <see cref="IAreaReloadRuntime"/>. This handler never references NWN objects
/// directly and never publishes through <see cref="IEventBus"/>.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<ReloadAreaCommand>))]
public sealed class ReloadAreaCommandHandler : ICommandHandler<ReloadAreaCommand>
{
    private readonly IAreaReloadRuntime _runtime;

    public ReloadAreaCommandHandler(IAreaReloadRuntime runtime)
    {
        _runtime = runtime;
    }

    public async Task<CommandResult> HandleAsync(ReloadAreaCommand command, CancellationToken cancellationToken = default)
    {
        AreaReloadResult outcome = await _runtime.ReloadAreaAsync(command.ResRef).ConfigureAwait(false);

        return outcome.Status switch
        {
            AreaReloadStatus.Reloaded => CommandResult.OkWithData(BuildData(outcome)),
            AreaReloadStatus.NotFound => CommandResult.Fail("area_not_found"),
            AreaReloadStatus.Occupied => CommandResult.Fail("area_occupied"),
            AreaReloadStatus.RecreateFailed => CommandResult.Fail("recreate_failed"),
            _ => CommandResult.Fail("area_reload_unknown"),
        };
    }

    private static Dictionary<string, object> BuildData(AreaReloadResult outcome) => new()
    {
        ["outcome"] = outcome.Status switch
        {
            AreaReloadStatus.NotFound => "area_not_found",
            AreaReloadStatus.Occupied => "area_occupied",
            AreaReloadStatus.Reloaded => "reloaded",
            AreaReloadStatus.RecreateFailed => "recreate_failed",
            _ => "area_reload_unknown",
        },
        ["resref"] = outcome.ResRef,
        ["name"] = outcome.Name ?? string.Empty,
        ["message"] = outcome.Message,
    };
}
