using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Emotes;

public class EmoteWindowPresenter : ScryPresenter<EmoteWindowView>
{
    private readonly NwPlayer _player;
    private EmoteModel Model { get; }

    private NuiWindow? _window;
    private NuiWindowToken _token;

    public EmoteWindowPresenter(EmoteWindowView emoteWindowView, NwPlayer player)
    {
        View = emoteWindowView;
        _player = player;
        Model = new EmoteModel(player);
    }

    public override EmoteWindowView View { get; }

    public override NuiWindowToken Token()
    {
        return _token;
    }

    public override void InitBefore()
    {
        Model.InitAllEmotes();
        // View.PopulateEmoteLayout(Model.Emotes.Values);
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(300, 300, 400, 500)
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
            _player.FloatingTextString("Failed to create window. Send a bug report.", false);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
    }

    public override void Close()
    {
    }
}