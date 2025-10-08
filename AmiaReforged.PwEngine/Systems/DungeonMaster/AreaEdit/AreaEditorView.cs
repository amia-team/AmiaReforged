using AmiaReforged.PwEngine.Systems.DungeonMaster.NpcBank;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster.AreaEdit;

public class AreaEditorView : ScryView<AreaEditorPresenter>, IDmWindow
{
    public override AreaEditorPresenter Presenter { get; protected set; }

    public string Title => "Area Editor";

    public AreaEditorView(NwPlayer player)
    {
        //nothing yet
    }

    public bool ListInDmTools => false;

    public override NuiLayout RootLayout()
    {
        return new NuiRow();
    }

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;
}

public class AreaEditorPresenter : ScryPresenter<AreaEditorView>
{
    public override AreaEditorView View { get; }
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly NwPlayer _player;

    public AreaEditorPresenter(AreaEditorView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
    }

    public override void Create()
    {
    }

    public override void Close()
    {
    }
}
