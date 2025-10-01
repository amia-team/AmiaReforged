using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster.NpcBank;

public class NpcBankPresenter : ScryPresenter<NpcBankView>
{
    public override NpcBankView View => _npcBankView;

    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly NpcBankView _npcBankView;
    private readonly NwPlayer _player;

    public NpcBankPresenter(NpcBankView npcBankView, NwPlayer player)
    {
        _npcBankView = npcBankView;
        _player = player;
        Model = new NpcBankModel(player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Model);
    }

    public override NuiWindowToken Token() => _token;
    private NpcBankModel Model { get; }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
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
            _player.SendServerMessage(
                message: "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        Token().SetBindValue(View.Search, "");

        Model.LoadNpcs();

        Model.NpcUpdate += UpdateList;

        Token().SetBindValue(View.Selection, 0);
        RefreshNpcList();
    }

    private void UpdateList(NpcBankModel sender, EventArgs e)
    {
        RefreshNpcList();
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
            RefreshNpcList();
        }
        else if (eventData.ElementId == View.MakePublicButton.Id
                 && eventData.ArrayIndex >= 0
                 && eventData.ArrayIndex < Model.VisibleNpcs.Count())
        {
            Model.TogglePublicSetting(eventData.ArrayIndex);
        }
        else if (eventData.ElementId == View.SpawnNpcButton.Id
                 && eventData.ArrayIndex >= 0
                 && eventData.ArrayIndex < Model.VisibleNpcs.Count())
        {
            int selected = Token().GetBindValue(View.Selection);
            StandardFaction selectedFaction = selected switch
            {
                0 => StandardFaction.Commoner,
                1 => StandardFaction.Merchant,
                2 => StandardFaction.Defender,
                3 => StandardFaction.Hostile,
                _ => StandardFaction.Commoner
            };

            NwFaction faction = NwFaction.FromStandardFaction(selectedFaction)!;
            Model.PromptSpawn(eventData.ArrayIndex, faction);
        }
        else if (eventData.ElementId == View.DeleteNpcButton.Id
                 && eventData.ArrayIndex >= 0
                 && eventData.ArrayIndex < Model.VisibleNpcs.Count())
        {
            Model.PromptForDelete(eventData.ArrayIndex);
        }
        else if (eventData.ElementId == View.AddNpcButton.Id)
        {
            Model.PromptAdd();
        }
    }

    private void RefreshNpcList()
    {
        string search = Token().GetBindValue(View.Search)!;

        Model.SetSearchTerm(search);
        Model.LoadNpcs();
        Model.RefreshNpcList();

        List<string> npcNames = [];
        npcNames.AddRange(Model.VisibleNpcs.Select(n => n.DmCdKey != _player.CDKey ? $"{n.Name} (Shared)" : n.Name));

        List<long> npcIds = Model.VisibleNpcs.Select(n => n.Id).ToList();

        List<string> publicTooltips = Model.VisibleNpcs.Select(visibleNpc => visibleNpc.Public
                ? "This NPC is visible to other DMs"
                : "Not visible to other DMs")
            .ToList();

        List<string> publicImageResrefs = Model.VisibleNpcs.Select(visibleNpc => visibleNpc.Public
                ? "ir_reldom"
                : "ief_blind")
            .ToList();

        Token().SetBindValues(View.Names, npcNames);
        Token().SetBindValues(View.PublicImageResref, publicImageResrefs);
        Token().SetBindValues(View.PublicSettings, publicTooltips);
        Token().SetBindValue(View.NpcCount, Model.VisibleNpcs.Count());
    }

    public override void Close()
    {
    }
}
