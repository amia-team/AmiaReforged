using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui;

public sealed class PlayerToolsWindowPresenter : ScryPresenter<PlayerToolsWindowView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public PlayerToolsWindowPresenter(PlayerToolsWindowView toolView, NwPlayer player)
    {
        _player = player;
        View = toolView;
        Model = new PlayerToolsModel(player);
    }

    [Inject] private Lazy<WindowDirector> WindowDirector { get; init; } = null!;
    [Inject] private Lazy<RuntimeCharacterService> PlayerIdService { get; init; } = null!;
    [Inject] private Lazy<CharacterService> CharacterService { get; init; } = null!;

    private PlayerToolsModel Model { get; }

    public override PlayerToolsWindowView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), title: "Player Tools")
        {
            Geometry = new NuiRect(120f, 100f, 680f, 680f),
            Resizable = true
        };
    }

    public override async void Create()
    {
        // Create the window if it's null.
        if (_window == null)
            // Try to create the window if it doesn't exist.
            InitBefore();

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            _player.SendServerMessage(
                message: "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
        Guid characterId = PlayerIdService.Value.GetPlayerKey(Token().Player);
        bool isPersisted = await CharacterService.Value.CharacterExists(characterId);
        await NwTask.SwitchToMainThread();
        Model.CharacterIsPersisted = isPersisted;

        if (!isPersisted)
            Token().Player
                .FloatingTextString(
                    message:
                    "You haven't gone through the entry area yet. You'll want to do this if you want access to all functionality.",
                    false);

        RefreshWindowList();
    }

    private void RefreshWindowList()
    {
        Model.SetSearchTerm(string.Empty);
        Model.RefreshWindowList();

        for (int i = 0; i < 20; i++)
        {
            if (i < Model.VisibleWindows.Count)
            {
                Token().SetBindValue(View.ToolNameBinds[i], Model.VisibleWindows[i].Title);
                Token().SetBindValue(View.ToolVisibleBinds[i], true);
                Token().SetBindValue(View.ToolEnabledBinds[i], Model.EnabledWindowIndices.Contains(i));

                string disabledTooltip = Model.DisabledReasons.TryGetValue(i, out string reason)
                    ? reason
                    : string.Empty;
                Token().SetBindValue(View.ToolDisabledTooltipBinds[i], disabledTooltip);
            }
            else
            {
                Token().SetBindValue(View.ToolNameBinds[i], string.Empty);
                Token().SetBindValue(View.ToolVisibleBinds[i], false);
                Token().SetBindValue(View.ToolEnabledBinds[i], false);
                Token().SetBindValue(View.ToolDisabledTooltipBinds[i], string.Empty);
            }
        }
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

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        // Check if it's one of the open tool buttons
        if (eventData.ElementId.StartsWith("btn_opentool_"))
        {
            string indexStr = eventData.ElementId.Replace("btn_opentool_", "");
            if (int.TryParse(indexStr, out int index) &&
                index >= 0 && index < Model.VisibleWindows.Count &&
                Model.EnabledWindowIndices.Contains(index))
            {
                IToolWindow window = Model.VisibleWindows[index];
                IScryPresenter toolWindow = window.ForPlayer(Token().Player);
                WindowDirector.Value.OpenWindow(toolWindow);
            }
        }
    }

    public override void Close()
    {
        Model.ClearVisibleWindows();
        _token.Close();
    }
}
