using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(ElementalTypeToggler))]
public class ElementalTypeToggler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ElementalTypeToggler()
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
        LocalVariableInt elementalType = monk.GetObjectVariable<LocalVariableInt>(MonkElemental.VarName);

        // On feat use, elemental type always switches to the next
        elementalType.Value++;
        if (elementalType.Value > MonkElemental.Earth) elementalType.Value = MonkElemental.Fire;

        string elementalName = elementalType.Value switch
        {
            MonkElemental.Fire => "Fire",
            MonkElemental.Water => "Water",
            MonkElemental.Air => "Air",
            MonkElemental.Earth => "Earth",
            _ => "Fire"
        };

        string elementalSound = elementalType.Value switch
        {
            MonkElemental.Fire => "sff_explfire",
            MonkElemental.Water => "as_na_splash1",
            MonkElemental.Air => "sco_mehansonc02",
            MonkElemental.Earth => "sff_rainice",
            _ => "sff_explfire"
        };

        Color elementalColor = elementalType.Value switch
        {
            MonkElemental.Fire => ColorConstants.Orange,
            MonkElemental.Water => ColorConstants.Cyan,
            MonkElemental.Air => ColorConstants.Silver,
            MonkElemental.Earth => ColorConstants.Green,
            _ => ColorConstants.Orange
        };

        monk.PlaySound(elementalSound);
        player.FloatingTextString($"*Activated {elementalName.ColorString(elementalColor)}*", false, false);
    }
}
