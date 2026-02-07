using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor;

[ServiceBinding(typeof(ElementalTypeToggler))]
public class ElementalTypeToggler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ElementalTypeToggler()
    {
        NwModule.Instance.OnUseFeat += ToggleElementalType;
        Log.Info(message: "Monk Elemental Type Toggler initialized.");
    }

    private static void ToggleElementalType(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not MonkFeat.PoeCrashingMeteor) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature monk = eventData.Creature;
        LocalVariableEnum<ElementalType> elementalTypeVar = MonkUtils.GetElementalTypeVar(monk);

        // On feat use, elemental type always switches to the next
        elementalTypeVar.Value++;
        if (elementalTypeVar.Value > ElementalType.Earth) elementalTypeVar.Value = ElementalType.Fire;

        string elementalName = elementalTypeVar.Value.ToString();

        Color elementalColor = elementalTypeVar.Value switch
        {
            ElementalType.Fire => ColorConstants.Orange,
            ElementalType.Water => ColorConstants.Cyan,
            ElementalType.Air => ColorConstants.Silver,
            ElementalType.Earth => ColorConstants.Green,
            _ => ColorConstants.Orange
        };

        player.FloatingTextString($"*Activated {elementalName.ColorString(elementalColor)}*", false, false);
    }
}
