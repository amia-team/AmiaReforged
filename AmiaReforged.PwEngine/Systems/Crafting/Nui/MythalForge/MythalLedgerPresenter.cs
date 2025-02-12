using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public class MythalLedgerPresenter : ScryPresenter<MythalLedgerView>
{
    private NuiWindow? _window;
    private readonly NwPlayer _player;
    private readonly MythalForgePresenter _parent;
    private NuiWindowToken _token;
    public override MythalLedgerView ToolView { get; }

    public MythalLedgerPresenter(MythalForgePresenter parent, NwPlayer player, MythalLedgerView toolView)
    {
        ToolView = toolView;
        _parent = parent;
        _player = player;

        parent.ViewUpdated += UpdateLedger;
        parent.ForgeClosing += HandleClose;
    }

    private void HandleClose(MythalForgePresenter sender, EventArgs e)
    {
        Close();
    }

    private void UpdateLedger(MythalForgePresenter sender, EventArgs e)
    {
        UpdateLedgerBindings(sender.Model);
    }

    private void UpdateLedgerBindings(MythalForgeModel senderModel)
    {
        Token().SetBindValue(ToolView.MinorMythalCount, senderModel.MythalCategoryModel.MinorMythals);
        Token().SetBindValue(ToolView.LesserMythalCount, senderModel.MythalCategoryModel.LesserMythals);
        Token().SetBindValue(ToolView.IntermediateMythalCount, senderModel.MythalCategoryModel.IntermediateMythals);
        Token().SetBindValue(ToolView.GreaterMythalCount, senderModel.MythalCategoryModel.GreaterMythals);
        Token().SetBindValue(ToolView.FlawlessMythalCount, senderModel.MythalCategoryModel.FlawlessMythals);
        Token().SetBindValue(ToolView.PerfectMythalCount, senderModel.MythalCategoryModel.PerfectMythals);
        Token().SetBindValue(ToolView.DivineMythalCount, senderModel.MythalCategoryModel.DivineMythals);
    }

    public override NuiWindowToken Token()
    {
        return _token;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(ToolView.RootLayout(), "Mythal Ledger")
        {
            Id = "mythal_ledger",
            Geometry = new NuiRect(1600, 500, 300, 300),
            Closable = false,
            Resizable = false
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            InitBefore();
        }

        if (_window == null)
        {
            _player.SendServerMessage("Failed to create Mythal Ledger window." +
                                      " Please screenshot this and file a bug report on the Discord.");
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
    }

    public override void Close()
    {
        _token.Close();
    }
}