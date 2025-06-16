using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

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
            Children = [
                new NuiRow()
                {
                    Children = [
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
    public bool ListInDmTools => true;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;
}

public class TileEditPresenter(TileEditView tileEditView, NwPlayer player) : ScryPresenter<TileEditView>
{
    public override TileEditView View { get; }
    public override NuiWindowToken Token() => throw new NotImplementedException();

    private NuiWindow? _window;
    
    public override void InitBefore()
    {
        _window = new(View.RootLayout(), title: View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f)
        };
    }

    public override void Create()
    {
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
    }

    public override void Close()
    {
        throw new NotImplementedException();
    }
}