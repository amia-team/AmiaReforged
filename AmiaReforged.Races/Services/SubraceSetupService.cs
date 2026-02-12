using AmiaReforged.Races.Races.Script.SubraceTemplates;
using AmiaReforged.Races.Races.Utils;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Races.Services;

[ServiceBinding(typeof(SubraceSetupService))]
public class SubraceSetupService
{
    private const string EntryGateTag = "ds_entrygate";

    private static readonly Dictionary<string, ISubraceApplier> Subraces = new()
    {
        { "aasimar", new AasimarOption() },
        { "tiefling", new TieflingOption() },
        { "earth genasi", new EarthGenasiOption() },
        { "earth", new EarthGenasiOption() },
        { "water genasi", new WaterGenasiOption() },
        { "water", new WaterGenasiOption() },
        { "fire genasi", new FireGenasiOption() },
        { "fire", new FireGenasiOption() },
        { "air genasi", new AirGenasiOption() },
        { "air", new AirGenasiOption() },
        { "feytouched", new FeytouchedOption() },
        { "aquatic elf", new AquaticElfOption() },
        { "feyri", new FeyriOption() },
        { "fey'ri", new FeyriOption() },
        { "shadovar", new ShadovarOption() },
        { "centaur", new CentaurOption() },
        { "avariel", new AvarielOption() },
        { "kenku", new KenkuOption() },
        { "lizardfolk", new LizardfolkOption() },
        { "half dragon", new HalfDragonOption() },
        { "half-dragon", new HalfDragonOption() }
    };

    private readonly IReadOnlyList<RacialType> _eligibleRaces = new List<RacialType>
    {
        RacialType.Human,
        RacialType.Elf,
        RacialType.Dwarf,
        RacialType.Halfling,
        RacialType.Gnome,
        RacialType.HalfElf,
        RacialType.HalfOrc
    };

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public SubraceSetupService()
    {
        NwPlaceable? entryGate = NwObject.FindObjectsWithTag<NwPlaceable>(EntryGateTag).FirstOrDefault();

        if (entryGate == null)
        {
            Log.Info("Subrace service failed to initialize.");
            return;
        }

        entryGate.OnUsed += SetupSubrace;

        Log.Info("Subrace setup service initialized.");
    }

    private void SetupSubrace(PlaceableEvents.OnUsed obj)
    {
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer player)) return;
        if (player.IsDM || player.IsPlayerDM) return;
        if (player.LoginCreature is null) return;
        if (TemplateItem.Initialized(player.LoginCreature)) return;

        if (!_eligibleRaces.Contains(player.LoginCreature.Race.RacialType))
        {
            player.SendServerMessage("This subrace selection is only available to the base Neverwinter Nights races.", ColorConstants.Red);
            return;
        }

        string usedBySubRace = obj.UsedBy.SubRace.ToLower();
        if (!Subraces.ContainsKey(usedBySubRace)) return;

        Subraces[usedBySubRace].Apply(player.LoginCreature);
    }
}
