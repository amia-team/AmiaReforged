using AmiaReforged.Core;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.System.Services;
using AmiaReforged.System.UI.PlayerTools.Spellbook.CreateSpellbook;
using AmiaReforged.System.UI.PlayerTools.Spellbook.OpenSpellbook;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.Spellbook;

public class SpellbookListController : WindowController<SpellbookListView>
{
    [Inject] private Lazy<WindowManager> WindowManager { get; set; }
    [Inject] private Lazy<SpellbookLoaderService> SpellbookLoader { get; set; }

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private List<SpellbookViewModel?> _spellbooks;
    private List<SpellbookViewModel?>? _visibleSpellbooks = new();

    public override void Init()
    {
        if (!Token.Player.LoginCreature.Classes.Any(c => c.Class is { IsSpellCaster: true, HasMemorizedSpells: true }))
        {
            Token.Player.SendServerMessage("You don't have any classes that memorize spells, so you don't have any spellbooks.");
            Token.Close();
            return;
        }
        string idString = NWScript.GetLocalString(Token.Player.LoginCreature, "pc_guid");
        Guid characterId = Guid.Parse(idString);

        if (characterId == Guid.Empty)
        {
            Token.Player.SendServerMessage(
                "Could not source your character, so functionality may be limited. If you don't have a PC key, you'll need to enter the travel agency and try again.");
            return; // This is a temporary measure until we have a better way to handle this.
        }

        _spellbooks = SpellbookLoader.Value.LoadSpellbook(characterId);

        RefreshSpellbookList();
    }

    public override async void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
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
        if (selectedSpellbook == null)
        {
            return;
        }

        NWScript.SetLocalString(Token.Player.LoginCreature, "selected_spellbook", selectedSpellbook.Id.ToString());
        Log.Info($"Stored spellbook id: {selectedSpellbook.Id}");
        WindowManager.Value.OpenWindow<OpenSpellbookView>(Token.Player);
        Token.Close();
    }

    private void DeleteSelectedSpellbook(ModuleEvents.OnNuiEvent eventData)
    {
        SpellbookViewModel? selectedSpellbook = _visibleSpellbooks?[eventData.ArrayIndex];
        if (selectedSpellbook == null)
        {
            return;
        }

        string pcIdString = NWScript.GetLocalString(Token.Player.LoginCreature, "pc_guid");
        Guid pcId = Guid.Parse(pcIdString);
        SpellbookLoader.Value.DeleteSpellbook(selectedSpellbook.Id, pcId);

        Token.Player.SendServerMessage($"Spellbook {selectedSpellbook.Name} has been deleted.");

        _spellbooks.Remove(selectedSpellbook);

        RefreshSpellbookList();
    }

    private void OpenCreateSpellbookWindow()
    {
        WindowManager.Value.OpenWindow<CreateSpellbookView>(Token.Player);
        Token.Close();
    }

    private void RefreshSpellbookList()
    {
        string search = Token.GetBindValue(View.Search)!;
        _visibleSpellbooks = _spellbooks.Where(book =>
                book != null && book.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<string> spellbookNames = _visibleSpellbooks.Select(view => view!.Name).ToList();
        List<string> spellbookIds = _visibleSpellbooks.Select(view => view!.Id.ToString()).ToList();

        Token.SetBindValues(View.SpellbookNames, spellbookNames);
        Token.SetBindValues(View.SpellbookIds, spellbookIds);
        Token.SetBindValue(View.SpellbookCount, _visibleSpellbooks.Count);
    }

    protected override void OnClose()
    {
        _visibleSpellbooks = null;
    }
}