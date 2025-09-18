using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster;

public sealed class DmToolPresenter(DmToolView view, NwPlayer player) : ScryPresenter<DmToolView>
{
    public override DmToolView View { get; } = view;
    public override NuiWindowToken Token() => _token;
    private NuiWindowToken _token;
    private NuiWindow? _window;


    [Inject] private Lazy<WindowDirector> WindowDirector { get; init; } = null!;
    [Inject] private Lazy<RuntimeCharacterService> PlayerIdService { get; init; } = null!;
    [Inject] private Lazy<CharacterService> CharacterService { get; init; } = null!;

    private DmToolsModel Model { get; } = new(player);

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), title: "DM Tools")
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


        player.TryCreateNuiWindow(_window, out _token);

        Token().SetBindValue(View.Search, string.Empty);
        RefreshWindowList();
    }

    private void RefreshWindowList()
    {
        string search = Token().GetBindValue(View.Search)!;

        Model.SetSearchTerm(search);
        Model.RefreshWindowList();

        List<string> windowNames = Model.VisibleWindows.Select(view => view.Title).ToList();
        Token().SetBindValues(View.WindowNames, windowNames);
        Token().SetBindValue(View.WindowCount, Model.VisibleWindows.Count);
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
        if (eventData.ElementId == View.SearchButton.Id)
        {
            RefreshWindowList();
        }
        else if (eventData.ElementId == View.OpenWindowButton.Id &&
                 eventData.ArrayIndex >= 0 && eventData.ArrayIndex < Model.VisibleWindows.Count)
        {
            IDmWindow window = Model.VisibleWindows[eventData.ArrayIndex];
            IScryPresenter toolWindow = window.ForPlayer(Token().Player);
            WindowDirector.Value.OpenWindow(toolWindow);
        }
    }

    public override void Close()
    {
        Model.ClearVisibleWindows();
    }
}