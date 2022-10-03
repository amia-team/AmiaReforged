using System.Diagnostics;
using Amia.Racial.Races.Script.SubraceTemplates;
using Amia.Racial.Races.Utils;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace Amia.Racial.Services;

[ServiceBinding(typeof(SubraceSetupService))]
public class SubraceSetupService
{
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

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public SubraceSetupService()
    {
        NwPlaceable? entryGate = NwObject.FindObjectsWithTag<NwPlaceable>("ds_entrygate").FirstOrDefault();

        if (entryGate == null)
        {
            Log.Info("Subrace service failed to initialize.");
            return;
        }

        entryGate.OnUsed += SetupSubrace;

        Log.Info("Subrace setup service initialized.");
    }

    private static void SetupSubrace(PlaceableEvents.OnUsed obj)
    {
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer player)) return;
        if (player.IsDM || player.IsPlayerDM) return;
        if (TemplateItem.Initialized(player.LoginCreature)) return;

        string usedBySubRace = obj.UsedBy.SubRace.ToLower();
        if (!Subraces.ContainsKey(usedBySubRace)) return;

        Subraces[usedBySubRace].Apply(player.LoginCreature);
    }
}