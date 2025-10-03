using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.PwEngine.Systems.Chat.Commands;
using Anvil.API;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Commands;

[ServiceBinding(typeof(IChatCommand))]
public class ConfirmPath : IChatCommand
{
    private readonly Dictionary<string, NwFeat?> _pathFeats = new()
    {
        { "cracked_vessel", NwFeat.FromFeatId(MonkFeat.PoeCrackedVessel) },
        { "crashing_meteor", NwFeat.FromFeatId(MonkFeat.PoeCrashingMeteor) },
        { "echoing_valley", NwFeat.FromFeatId(MonkFeat.PoeEchoingValley) },
        { "fickle_strand", NwFeat.FromFeatId(MonkFeat.PoeFickleStrand) },
        { "hidden_spring", NwFeat.FromFeatId(MonkFeat.PoeHiddenSpring) },
        { "ironclad_bull", NwFeat.FromFeatId(MonkFeat.PoeIroncladBull) },
        { "swinging_censer", NwFeat.FromFeatId(MonkFeat.PoeSwingingCenser) }
    };

    private static readonly NwClass? ObsoletePoeClass = NwClass.FromClassId(50);
    public string Command => "./confirmpath";
    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        NwCreature? monk = caller.ControlledCreature;
        if (monk == null) return Task.CompletedTask;

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (MonkUtils.GetMonkPath(monk) != null && environment == "live")
        {
            caller.FloatingTextString
            ("Path of Enlightenment has already been selected. " +
             "Rebuilding allows you to reselect your path.", false);

            return Task.CompletedTask;
        }

        if (monk.GetClassInfo(ObsoletePoeClass) != null)
        {
            caller.FloatingTextString
            ("Obsolete Path of Enlightenment prestige class found. " +
             "Rebuilding without that class allows you to select your path.", false);

            return Task.CompletedTask;
        }

        string[] validKeysArray = _pathFeats.Keys.ToArray();
        string validKeys = string.Join(", ", validKeysArray);
        string validationMessage = $"Please specify a valid path. Valid inputs: ./confirmpath {validKeys}";

        if (args.Length < 1)
        {
            caller.SendServerMessage($"{validationMessage}");

            return Task.CompletedTask;
        }

        string monkPathKey = args[0];

        if (!_pathFeats.TryGetValue(monkPathKey, out NwFeat? chosenFeat))
        {
            caller.SendServerMessage($"{validationMessage}");

            return Task.CompletedTask;
        }

        if (chosenFeat == null) return Task.CompletedTask;

        if (environment != "live")
        {
            NwFeat? existingPath = monk.Feats.FirstOrDefault(f => _pathFeats.ContainsValue(f));
            if (existingPath != null) monk.RemoveFeat(existingPath);
        }

        monk.AddFeat(chosenFeat, 12);

        caller.FloatingTextString
            ($"Path of Enlightenment selected permanently: {chosenFeat.Name}", false);

        return Task.CompletedTask;
    }
}
