using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(MonkPathSelector))]
public class MonkPathSelector
{
    private readonly Dictionary<int, string> _pathDescriptions = new()
    {
        { 1, CrackedVessel },
        { 2, CrashingMeteor },
        { 3, EchoingValley },
        { 4, FickleStrand },
        { 5, HiddenSpring },
        { 6, IroncladBull },
        { 7, SwingingCenser }
    };

    private readonly Dictionary<int, string> _pathKeys = new()
    {
        { 1, "cracked_vessel" },
        { 2, "crashing_meteor" },
        { 3, "echoing_valley" },
        { 4, "fickle_strand" },
        { 5, "hidden_spring" },
        { 6, "ironclad_bull" },
        { 7, "swinging_censer" }
    };

    private const string PathDescriptionKey = "monk_path";

    private readonly Logger _log = LogManager.GetCurrentClassLogger();

    public MonkPathSelector()
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment == "live") return;

        NwModule.Instance.OnUseFeat += SelectMonkPath;

        _log.Info("Monk Path Selector initialized.");
    }

    private void SelectMonkPath(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not MonkFeat.PoeBase) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature monk = eventData.Creature;

        if (MonkUtils.GetMonkPath(monk) != null) return;

        TogglePath(monk, player);
    }

    private void TogglePath(NwCreature monk, NwPlayer player)
    {
        int descriptionKey = monk.GetObjectVariable<LocalVariableInt>(PathDescriptionKey).Value;

        // Reset key to first choice if it goes past the last path
        descriptionKey = descriptionKey % 7 + 1;

        string pathMessage = "Path of Enlightenment: ";
        pathMessage += _pathDescriptions[descriptionKey];
        pathMessage += "\n\nTo confirm this path as your choice, enter this command in the chat: " +
                       $"./confirmpath {_pathKeys[descriptionKey]}";

        player.SendServerMessage($"{pathMessage}".ColorString(ColorConstants.Teal));
    }

    private const string CrackedVessel =
        "Cracked Vessel\n\nSeek clarity and power through the ritual infliction of pain. " +
        "This path shares your suffering with foes, dividing your own to multiply theirs.";

    private const string CrashingMeteor =
        "Crashing Meteor\n\nBecome as the elements and an unstoppable force of nature. " +
        "This path channels Ki through elemental forces to deliver devastating strikes";

    private const string EchoingValley =
        "Echoing Valley\n\nBecome a conduit for the Ki of all living things which yearn to manifest through " +
        "your will. This path summons Echoes - fractions of Ki that travel and fight by your side.";

    private const string FickleStrand =
        "Fickle Strand\n\nLike pulling a loose thread, you tug at the Weave with your Ki. This path unravels " +
        "magical defenses and reshapes magic into unpredictable magical strikes.";

    private const string HiddenSpring =
        "Hidden Spring\n\nIn stillness, you uncover inner strength and awaken an inner sight. " +
        "This path lets you anticipate the future flow of the fight and strike with peerless foresight.";

    private const string IroncladBull =
        "Ironclad Bull\n\nLike a stalwart gorgon, swords bounce off your skin from the sheer flow of Ki. " +
        "This path seperates body from mind, turning you into an impenetrable fortress.";

    private const string SwingingCenser =
        "Swinging Censer\n\nListen to the heartbeat of Ki that courses through all living things. " +
        "This path is about striking powerful rhythmic stances that bolster allies and mend wounds.";
}
