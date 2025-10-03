using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.PwEngine.Systems.Chat.Commands;
using Anvil.API;
using Anvil.Services;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace AmiaReforged.Classes.Monk.Commands;

[ServiceBinding(typeof(IChatCommand))]
public class ConfirmFightingStyle : IChatCommand
{
    private readonly Dictionary<string, NwFeat[]> _fightingStyleFeats = new()
    {
        { "knockdown", [NwFeat.FromFeatType(Feat.Knockdown)!, NwFeat.FromFeatType(Feat.ImprovedKnockdown)!] },
        { "disarm", [NwFeat.FromFeatType(Feat.Disarm)!, NwFeat.FromFeatType(Feat.ImprovedDisarm)!] },
        { "ranged", [NwFeat.FromFeatType(Feat.CalledShot)!, NwFeat.FromFeatType(Feat.Mobility)!] }
    };

    public string Command => "./confirmstyle";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        NwCreature? monk = caller.ControlledCreature;
        if (monk == null) return Task.CompletedTask;

        if (HasMonkFightingStyle(monk, caller)) return Task.CompletedTask;

        string[] validKeysArray = _fightingStyleFeats.Keys.ToArray();
        string validKeys = string.Join(", ", validKeysArray);
        string validationMessage = $"Please specify a valid fighting style. Valid inputs: ./confirmstyle {validKeys}";

        if (args.Length < 1)
        {
            caller.SendServerMessage($"{validationMessage}");

            return Task.CompletedTask;
        }

        string fightingStyleKey = args[0];

        if (!_fightingStyleFeats.TryGetValue(fightingStyleKey, out NwFeat[]? featsToAdd))
        {
            caller.SendServerMessage($"{validationMessage}");

            return Task.CompletedTask;
        }

        if (featsToAdd.All(f => monk.KnowsFeat(f)))
        {
            caller.SendServerMessage("You already know both feats of this fighting style. Select another style.");

            return Task.CompletedTask;
        }

        // Check if the monk already has one of the feats they're selecting and ask for confirmation
        bool hasOneFeat = featsToAdd.Any(f => monk.KnowsFeat(f));

        if (hasOneFeat && (args.Length < 2 || args[1] != "confirm"))
        {
            caller.SendServerMessage("You already know one of the feats for this fighting style. " +
                                     "If you are sure you want to select this path, enter this in the chat: " +
                                     $"\n./confirmstyle {fightingStyleKey} confirm");

            return Task.CompletedTask;
        }

        foreach (NwFeat feat in featsToAdd)
        {
            monk.AddFeat(feat, 6);
        }

        caller.FloatingTextString($"Added feats {featsToAdd[0].Name} and {featsToAdd[1].Name}");

        return Task.CompletedTask;
    }

    private bool HasMonkFightingStyle(NwCreature monk, NwPlayer caller)
    {
        if (monk.Level < 6) return true;

        NwFeat[] level6Feats = monk.LevelInfo[6].Feats.ToArray();

        if (level6Feats.OrderBy(f => f.Id).SequenceEqual(_fightingStyleFeats["knockdown"].OrderBy(f => f.Id)))
        {
            caller.SendServerMessage
                ("You have already selected Knockdown and Improved Knockdown feats for your Fighting Style.");

            return true;
        }
        if (level6Feats.OrderBy(f => f.Id).SequenceEqual(_fightingStyleFeats["disarm"].OrderBy(f => f.Id)))
        {
            caller.SendServerMessage
                ("You have already selected Disarm and Improved Disarm feats for your Fighting Style.");

            return true;
        }
        if (level6Feats.OrderBy(f => f.Id).SequenceEqual(_fightingStyleFeats["ranged"].OrderBy(f => f.Id)))
        {
            caller.SendServerMessage
                ("You have already selected Called Shot and Mobility feats for your Fighting Style.");

            return true;
        }

        return false;
    }
}
