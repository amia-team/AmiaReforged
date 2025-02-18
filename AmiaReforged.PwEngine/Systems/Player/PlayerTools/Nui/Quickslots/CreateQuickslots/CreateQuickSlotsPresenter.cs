using AmiaReforged.Core.Services;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Quickslots.CreateQuickslots;

public class CreateQuickSlotsPresenter : ScryPresenter<CreateQuickslotsView>
{
    private NuiWindow? _window;
    private NwPlayer _player;
    private NuiWindowToken _token;
    [Inject] private Lazy<QuickslotLoader> QuickslotLoader { get; set; }
    [Inject] private Lazy<WindowDirector> WindowManager { get; set; }


    public CreateQuickSlotsPresenter(CreateQuickslotsView toolView, NwPlayer player)
    {
        _player = player;
        View = toolView;
    }
    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(eventData);
                break;
        }
    }

    public override NuiWindowToken Token()
    {
        return _token;
    }

    public override CreateQuickslotsView View { get; }
    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0, 0, 400, 300),
            Closable = true,
            Resizable = false,
            Collapsed = false,
        };
    }

    public override void Create()
    {
        // Create the window if it's null.
        if (_window == null)
        {
            // Try to create the window if it doesn't exist.
            InitBefore();
        }

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            _player.SendServerMessage("The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        Token().SetBindValue(View.QuickslotName, string.Empty);

    }

    private async void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        try
        {
            if (eventData.ElementId == View.CreateButton.Id)
            {
                await SaveQuickslots();
                await NwTask.SwitchToMainThread();
            }
            else if (eventData.ElementId == View.CancelButton.Id)
            {
                Token().Close();
            }
        }
        catch (Exception e)
        {
            LogManager.GetCurrentClassLogger().Error("Error handling button click: " + e.Message);
        }
    }

    private async Task SaveQuickslots()
    {
        NwPlayer tokenPlayer = Token().Player;
        Guid playerId = PcKeyUtils.GetPcKey(tokenPlayer);
        byte[] serializedQuickbar = tokenPlayer.LoginCreature!.SerializeQuickbar()!;
        
        if (Token().GetBindValue(View.QuickslotName) == string.Empty)
        {
            tokenPlayer.SendServerMessage("You must enter a name for the quickslot.", ColorConstants.Red);
            return;
        }

        await QuickslotLoader.Value.SavePlayerQuickslots(Token().GetBindValue(View.QuickslotName)!, serializedQuickbar, playerId);
        await NwTask.SwitchToMainThread();

        Token().Close();
    }

    public override void Close()
    {
        Token().SetBindValue(View.QuickslotName, string.Empty);
    }
}