using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui;

public interface IToolWindow
{
    public string Id { get; }
    public bool ListInPlayerTools { get; }
    public bool RequiresPersistedCharacter { get; }
    public string Title { get; }
    public string CategoryTag { get; }

    public IScryPresenter ForPlayer(NwPlayer player);
}