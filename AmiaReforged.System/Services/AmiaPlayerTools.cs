using System.Runtime.InteropServices;
using AmiaReforged.Core.Helpers;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(AmiaPlayerTools))]
public class AmiaPlayerTools
{
    private readonly PlayerToolLayout _playerToolLayout;
    
    private readonly SpellbookService _spellbookService;
    private readonly CharacterListLayout _characterListLayout;

    public AmiaPlayerTools(PlayerToolLayout playerToolLayout, CharacterListLayout characterListLayout)
    {
        NwModule.Instance.OnClientEnter += AddPlayerToolButton;
        NwModule.Instance.OnNuiEvent += OnToolButtonClick;
        _playerToolLayout = playerToolLayout;
        _spellbookService = new SpellbookService();
        _characterListLayout = characterListLayout;
    }

    private async void OnToolButtonClick(ModuleEvents.OnNuiEvent obj)
    {
        if (obj is { ElementId: "spellbooksButton", EventType: NuiEventType.MouseUp }) _spellbookService.OpenSpellbookWindow(obj);
        if (obj is { ElementId: "viewCharactersButton", EventType: NuiEventType.MouseUp }) await _characterListLayout.OpenCharacterListWindow(obj);
    }

    private async void AddPlayerToolButton(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.IsDM) return;

        NuiLayout layout = await _playerToolLayout.CreateNuiLayout();

        NwTaskHelper taskHelper = new();

        await taskHelper.TrySwitchToMainThread();

        NuiWindow window = new(layout, "Player Tools")
        {
            Closable = true,
            Geometry = new NuiRect(500f, 100f, 300f, 400f)
        };

        obj.Player.TryCreateNuiWindow(window, out NuiWindowToken token);
    }
}