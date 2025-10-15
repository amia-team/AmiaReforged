using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster;

public interface IDmWindow
{
    string Title { get; }
    public bool ListInDmTools { get; }
    IScryPresenter ForPlayer(NwPlayer player);
}
