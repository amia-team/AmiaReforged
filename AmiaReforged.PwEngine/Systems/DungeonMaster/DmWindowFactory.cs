using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster;

public static class DmWindowFactory
{
    public static DmToolPresenter OpenDmTools(NwPlayer player)
    {
        DmToolPresenter dmToolPresenter = new DmToolView(player).Presenter;

        return dmToolPresenter;
    }
}