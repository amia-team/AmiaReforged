// Handles monk's elemental toggle ability on feat use

using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(ElementalToggleHandler))]
public class ElementalToggleHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ElementalToggleHandler()
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;

        NwModule.Instance.OnUseFeat += ToggleElementalType;
        Log.Info(message: "Monk Elemental Toggle Handler initialized.");
    }

    private static void ToggleElementalType(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not MonkFeat.PoeCrashingMeteor) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature monk = eventData.Creature;
        int elementalType = monk.GetObjectVariable<LocalVariableInt>(MonkElemental.VarName).Value;

        // On feat use, elemental type always switches to the next
        elementalType++;
        if (elementalType > MonkElemental.Earth) elementalType = MonkElemental.Fire;

        string elementalName = elementalType switch
        {
            MonkElemental.Fire => "Fire",
            MonkElemental.Water => "Water",
            MonkElemental.Air => "Air",
            MonkElemental.Earth => "Earth",
            _ => "Fire"
        };

        string elementalSound = elementalType switch
        {
            MonkElemental.Fire => "sff_explfire",
            MonkElemental.Water => "as_na_splash1",
            MonkElemental.Air => "sco_mehansonc02",
            MonkElemental.Earth => "sff_rainice",
            _ => "sff_explfire"
        };

        Color elementalColor = elementalType switch
        {
            MonkElemental.Fire => ColorConstants.Orange,
            MonkElemental.Water => ColorConstants.Cyan,
            MonkElemental.Air => ColorConstants.Silver,
            MonkElemental.Earth => ColorConstants.Green,
            _ => ColorConstants.Orange
        };
        elementalName.ColorString(elementalColor);

        monk.PlaySound(elementalSound);
        player.FloatingTextString($"*Activated {elementalName}*", false, false);
    }
}
