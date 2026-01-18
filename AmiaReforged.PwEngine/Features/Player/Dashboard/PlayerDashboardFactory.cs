using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard;

public static class PlayerDashboardFactory
{
    public static IScryPresenter OpenDashboard(NwPlayer player)
    {
        PlayerDashboardView view = new(player);
        return view.Presenter;
    }
}
