using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster;

public interface IDmWindow
{
    string Title { get; }
    public bool ListInDmTools { get; }
    IScryPresenter ForPlayer(NwPlayer player);
}