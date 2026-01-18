using AmiaReforged.PwEngine.Features.Player.Dashboard;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Player;

/// <summary>
/// Command to open the player dashboard
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class DashboardCommand : IChatCommand
{
    private readonly PlayerDashboardService _dashboardService;

    public DashboardCommand(PlayerDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public string Command => "./dashboard";
    public string Description => "Opens the player dashboard";
    public string AllowedRoles => "Player";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (caller.IsDM) return Task.CompletedTask;

        return _dashboardService.OpenDashboard(caller);
    }
}
