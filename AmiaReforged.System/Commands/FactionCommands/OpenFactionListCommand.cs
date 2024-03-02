using AmiaReforged.Core.Models;
using AmiaReforged.Core.Models.Faction;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.System.Commands.FactionCommands;

// [ServiceBinding(typeof(IChatCommand))]
public class OpenFactionListCommand : IChatCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly FactionService _factionService;
    private List<NuiElement> _nuiElementsFromFactions = null!;
    public string Command => "./factions";

    public OpenFactionListCommand(FactionService factionService)
    {
        _factionService = factionService;
    }

    public async Task ExecuteCommand(NwPlayer caller, string message)
    {
        _nuiElementsFromFactions = await FactionListToNuiElements();

        NuiLayout layout = await CreateNuiLayout();

        NuiWindow window = new(layout, "Faction List")
        {
            Closable = true,
            Geometry = new NuiRect(500f, 100f, 500f, 720f)
        };

        caller.TryCreateNuiWindow(window, out NuiWindowToken token);
    }

    private Task<NuiLayout> CreateNuiLayout()
    {
        NuiColumn root = new()
        {
            Children = _nuiElementsFromFactions
        };
        return Task.FromResult<NuiLayout>(root);
    }

    private async Task<List<NuiElement>> FactionListToNuiElements()
    {
        IEnumerable<FactionEntity> factions = await _factionService.GetAllFactions();

        List<NuiElement> elements = factions.Select(faction => new NuiRow
        {
            Id = faction.Name, Height = 40f,
            Children = new List<NuiElement>
            {
                new NuiLabel(faction.Name)
                    { Visible = true, VerticalAlign = NuiVAlign.Middle, HorizontalAlign = NuiHAlign.Left },
                new NuiText(faction.Description) { Visible = true, Id = faction.Name + "_Desc" },
                new NuiButton("X")
                {
                    Enabled = true,
                    Id = "delete_" + faction.Name
                }
            },
        }).Cast<NuiElement>().ToList();

        return elements;
    }
}