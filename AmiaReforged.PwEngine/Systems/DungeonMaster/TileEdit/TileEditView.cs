using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.GenericWindows;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster.TileEdit;

public sealed class TileEditView : ScryView<TileEditPresenter>, IDmWindow
{
    public override TileEditPresenter Presenter { get; protected set; }

    public NuiButton TileLeftButton = null!;
    public NuiButton TileRightButton = null!;


    public TileEditView(NwPlayer player)
    {
        Presenter = new TileEditPresenter(this, player);
    }

    public override NuiLayout RootLayout()
    {
        return new NuiColumn()
        {
            Children =
            [
                new NuiRow()
                {
                    Children =
                    [
                        new NuiButton("<")
                        {
                            Id = "cycle_tile_left"
                        }.Assign(out TileLeftButton)
                    ]
                }
            ]
        };
    }

    public string Title => "Tile Editor";
    public bool ListInDmTools => false;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;
}

public class TileEditPresenter(TileEditView tileEditView, NwPlayer player) : ScryPresenter<TileEditView>
{
    public override TileEditView View { get; } = tileEditView;
    public override NuiWindowToken Token() => _token;

    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override void InitBefore()
    {
        _window = new(View.RootLayout(), title: View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f)
        };

        if (player.LoginCreature == null) return;

        player.OnDMPossess += CloseOnPossess;
        player.OnDMPossessFullPower += CloseOnFullPossess;
    }

    private void CloseOnFullPossess(OnDMPossessFullPower obj)
    {
        OpenPopup();
        Close();
    }

    private void CloseOnPossess(OnDMPossess obj)
    {
        OpenPopup();
        Close();
    }

    private void OpenPopup()
    {
        GenericWindow
            .Builder()
            .For()
            .SimplePopup()
            .WithPlayer(player)
            .WithTitle("Unpossess Your Creature!")
            .WithMessage("You cannot use this while possessing a creature")
            .Open();
    }

    public override void Create()
    {
        if (player.LoginCreature == null)
        {
            OpenPopup();

            return;
        }

        // Create the window if it's null.
        if (_window == null)
            // Try to create the window if it doesn't exist.
            InitBefore();

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            player.SendServerMessage(
                message: "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        player.TryCreateNuiWindow(_window, out _token);
    }

    public override void Close()
    {
        player.OnDMPossess -= CloseOnPossess;
        player.OnDMPossessFullPower -= CloseOnFullPossess;
        _token.Close();
    }
}