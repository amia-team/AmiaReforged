using AmiaReforged.Core.Services;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.Player.PlayerId;
using AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Spellbook.CreateSpellbook;
using AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Spellbook.OpenSpellbook;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Spellbook;

public class SpellbookListPresenter : ScryPresenter<SpellbookListView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;

    private List<SpellbookViewModel?> _spellbooks = null!;

    private NuiWindowToken _token;
    private List<SpellbookViewModel?>? _visibleSpellbooks = new();
    private NuiWindow? _window;

    public SpellbookListPresenter(SpellbookListView toolView, NwPlayer player)
    {
        View = toolView;
        _player = player;
    }

    [Inject] private Lazy<WindowDirector> WindowDirector { get; set; } = null!;
    [Inject] private Lazy<SpellbookLoaderService> SpellbookLoader { get; set; } = null!;
    [Inject] private Lazy<PlayerIdService> PlayerIdService { get; set; } = null!;

    public override SpellbookListView View { get; }

    public override void InitBefore()
    {
        _window = new(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f)
        };
    }

    public override NuiWindowToken Token() => _token;

    public override void Create()
    {
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

        NwCreature? character = Token().Player.LoginCreature;

        if (character == null) return;

        if (!character.Classes.Any(c => c.Class is { IsSpellCaster: true, HasMemorizedSpells: true }))
        {
            Token().Player
                .SendServerMessage(
                    message: "You don't have any classes that memorize spells, so you don't have any spellbooks.");
            Token().Close();
            return;
        }

        Guid characterId = PlayerIdService.Value.GetPlayerKey(Token().Player);

        if (characterId == Guid.Empty)
        {
            Token().Player.SendServerMessage(
                message:
                "Could not source your character, so functionality may be limited. If you don't have a PC key, you'll need to enter the travel agency and try again.");
            return; // This is a temporary measure until we have a better way to handle this.
        }

        _spellbooks = SpellbookLoader.Value.LoadSpellbook(characterId);
        Token().SetBindValue(View.Search, string.Empty);
        RefreshSpellbookList();
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

    private async void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == View.SearchButton.Id)
        {
            RefreshSpellbookList();
        }
        else if (eventData.ElementId == View.CreateSpellbookButton.Id)
        {
            OpenCreateSpellbookWindow();
        }
        else if (eventData.ElementId == View.DeleteSpellbookButton.Id)
        {
            DeleteSelectedSpellbook(eventData);
            RefreshSpellbookList();
        }
        else if (eventData.ElementId == View.OpenSpellbookButton.Id)
        {
            OpenSelectedSpellbook(eventData);
        }
    }

    private void OpenSelectedSpellbook(ModuleEvents.OnNuiEvent eventData)
    {
        SpellbookViewModel? selectedSpellbook = _visibleSpellbooks?[eventData.ArrayIndex];
        if (selectedSpellbook == null) return;

        NWScript.SetLocalString(Token().Player.LoginCreature, sVarName: "selected_spellbook",
            selectedSpellbook.Id.ToString());
        Log.Info($"Stored spellbook id: {selectedSpellbook.Id}");

        OpenSpellbookView view = new(Token().Player);
        OpenSpellbookPresenter presenter = view.Presenter;
        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            Token().Player
                .SendServerMessage(
                    message:
                    "Failed to load the spellbook due to missing DI container. Screenshot this and report it as a bug.");
            return;
        }

        injector.Inject(presenter);

        WindowDirector.Value.OpenWindow(presenter);
        Token().Close();
    }

    private void DeleteSelectedSpellbook(ModuleEvents.OnNuiEvent eventData)
    {
        SpellbookViewModel? selectedSpellbook = _visibleSpellbooks?[eventData.ArrayIndex];
        if (selectedSpellbook == null) return;


        Guid pcId = PlayerIdService.Value.GetPlayerKey(Token().Player);
        if (pcId == Guid.Empty)
        {
            Token().Player.SendServerMessage("Couldn't delete the selected spellbook. Please file a bug report");
            return;
        }
        
        SpellbookLoader.Value.DeleteSpellbook(selectedSpellbook.Id, pcId);

        Token().Player.SendServerMessage($"Spellbook {selectedSpellbook.Name} has been deleted.");

        _spellbooks.Remove(selectedSpellbook);

        RefreshSpellbookList();
    }

    private void OpenCreateSpellbookWindow()
    {
        CreateSpellbookView view = new(_player);

        CreateSpellbookPresenter presenter = view.Presenter;
        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            _player.SendServerMessage(
                message:
                "Failed to load the spellbook creator due to missing DI container. Screenshot this and report it as a bug.");
            return;
        }

        injector.Inject(presenter);
        WindowDirector.Value.OpenWindow(presenter);
        Token().Close();
    }

    private void RefreshSpellbookList()
    {
        string search = Token().GetBindValue(View.Search) ?? string.Empty;
        _visibleSpellbooks = _spellbooks.Where(book =>
                book != null && book.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<string> spellbookNames = _visibleSpellbooks.Select(view => view!.Name).ToList();
        List<string> spellbookIds = _visibleSpellbooks.Select(view => view!.Id.ToString()).ToList();

        Token().SetBindValues(View.SpellbookNames, spellbookNames);
        Token().SetBindValues(View.SpellbookIds, spellbookIds);
        Token().SetBindValue(View.SpellbookCount, _visibleSpellbooks.Count);
    }

    public override void Close()
    {
        _visibleSpellbooks = null;
    }
}