using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using JetBrains.Annotations;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster.EncounterBuilder;

[UsedImplicitly]
public class EncounterBuilderPresenter(EncounterBuilderView view, NwPlayer player) : ScryPresenter<EncounterBuilderView>
{
    public override EncounterBuilderView View => view;

    private NuiWindowToken _token;
    private NuiWindow? _window;
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f)
        };
    }

    public override void Create()
    {
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
    }
}