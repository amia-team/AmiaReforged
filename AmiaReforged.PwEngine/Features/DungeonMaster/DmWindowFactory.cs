using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster;

public static class DmWindowFactory
{
    public static DmToolPresenter OpenDmTools(NwPlayer player)
    {
        DmToolPresenter dmToolPresenter = new DmToolView(player).Presenter;

        return dmToolPresenter;
    }
}
