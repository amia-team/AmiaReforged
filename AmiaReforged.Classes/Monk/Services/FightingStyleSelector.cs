using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(FightingStyleSelector))]
public class FightingStyleSelector
{
    private readonly Dictionary<int, string> _styleDescriptions = new()
    {
        { 1, Knockdown },
        { 2, Disarm },
        { 3, Ranged },
    };

    private const string FightingStyleDescriptionKey = "monk_fighting_style";

    private readonly Logger _log = LogManager.GetCurrentClassLogger();

    public FightingStyleSelector()
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment == "live") return;

        NwModule.Instance.OnUseFeat += SelectFightingStyle;

        _log.Info("Monk Fighting Style Selector initialized.");
    }

    private void SelectFightingStyle(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not MonkFeat.MonkFightingStyle) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature monk = eventData.Creature;

        ToggleFightingStyle(monk, player);
    }

    private void ToggleFightingStyle(NwCreature monk, NwPlayer player)
    {
        int descriptionKey = monk.GetObjectVariable<LocalVariableInt>(FightingStyleDescriptionKey).Value;

        // Reset key to first choice if it goes past the last style
        descriptionKey = descriptionKey % 3 + 1;

        player.SendServerMessage($"{_styleDescriptions[descriptionKey]}".ColorString(ColorConstants.Teal));
    }

    private const string Knockdown =
        "Knockdown style gains the feats Knockdown and Improved Knockdown. To select this as your permanent option, " +
        "enter this in the chat: ./confirmstyle knockdown";

    private const string Disarm =
        "Disarm style gains the feats Disarm and Improved Disarm. To select this as your permanent option, " +
        "enter this in the chat: ./confirmstyle disarm";

    private const string Ranged =
        "Ranged style gains the feats Called Shot and Mobility. To select this as your permanent option, " +
        "enter this in the chat: ./confirmstyle ranged";
}
