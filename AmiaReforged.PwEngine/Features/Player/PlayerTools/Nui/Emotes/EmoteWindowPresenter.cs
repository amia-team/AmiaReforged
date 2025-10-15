using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.Emotes;

public class EmoteWindowPresenter : ScryPresenter<EmoteWindowView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;

    private NuiWindow? _window;

    public EmoteWindowPresenter(EmoteWindowView emoteWindowView, NwPlayer player)
    {
        View = emoteWindowView;
        _player = player;
        Model = new EmoteModel(player);
    }

    private EmoteModel Model { get; }

    public override EmoteWindowView View { get; }

    public override NuiWindowToken Token() => _token;

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
        if (_window == null) InitBefore();

        if (_window == null)
        {
            _player.FloatingTextString(message: "Failed to create window. Send a bug report.", false);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
    }

    public override void Close()
    {
    }
}
