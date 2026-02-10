using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;
using ClassType = Anvil.API.ClassType;
using Feat = Anvil.API.Feat;

namespace AmiaReforged.Classes.Monk.Services;

/// <summary>
/// This service is meant to do all the feat shuffling for existing monks so they match the current monk feat progression.
/// For buildy reasons, monks may still want to rebuild. Also removes the obsolete POE prestige class.
/// </summary>
[ServiceBinding(typeof(MonkValidator))]
public class MonkValidator
{
    private static readonly NwClass? ObsoletePoeClass = NwClass.FromClassId(50);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly Dictionary<int, NwFeat?> MonkFeatsByLevel = new()
    {
        [2] = NwFeat.FromFeatId(MonkFeat.BindingStrike),
        [3] = NwFeat.FromFeatId(MonkFeat.MonkDefense),
        [4] = NwFeat.FromFeatId(MonkFeat.MonkSpeedNew),
        [5] = NwFeat.FromFeatId(MonkFeat.EagleStrike),
        [6] = NwFeat.FromFeatId(MonkFeat.MonkFightingStyle),
        [7] = NwFeat.FromFeatId(MonkFeat.WholenessOfBodyNew),
        [11] = NwFeat.FromFeatId(MonkFeat.KiBarrier),
        [12] = NwFeat.FromFeatId(MonkFeat.PoeBase),
        [13] = NwFeat.FromFeatType(Feat.DiamondSoul),
        [14] = NwFeat.FromFeatId(MonkFeat.AxiomaticStrike),
        [15] = NwFeat.FromFeatId(MonkFeat.EmptyBodyNew),
        [16] = NwFeat.FromFeatId(MonkFeat.QuiveringPalmNew),
        [17] = NwFeat.FromFeatId(MonkFeat.KiShout),
        [18] = NwFeat.FromFeatId(MonkFeat.KiStrike),
        [24] = NwFeat.FromFeatId(MonkFeat.KiStrike2),
        [30] = NwFeat.FromFeatId(MonkFeat.KiStrike3)
    };

    private static readonly HashSet<NwFeat?> OldFeats =
    [
        NwFeat.FromFeatType(Feat.MonkEndurance),
        NwFeat.FromFeatType(Feat.MonkAcBonus),
        NwFeat.FromFeatType(Feat.WholenessOfBody),
        NwFeat.FromFeatType(Feat.EmptyBody),
        NwFeat.FromFeatType(Feat.QuiveringPalm)
    ];

    public MonkValidator(EventService eventService)
    {
        eventService.SubscribeAll<OnLoadCharacterFinish, OnLoadCharacterFinish.Factory>(SortFeats, EventCallbackType.After);
        Log.Info(message: "Monk Validator Service initialized.");
    }

    private void SortFeats(OnLoadCharacterFinish eventData)
    {
        if (eventData.Player.LoginCreature is not { } monk
            || NWScript.GetLevelByClass(NWScript.CLASS_TYPE_MONK, monk) <= 0)
            return;

        int totalMonkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        List<string> removalReport = [];
        List<string> additionReport = [];

        PurgeInvalidFeats(monk, totalMonkLevel, removalReport);

        int monkLevelCounter = 0;
        int firstPoeLevel = 0;
        for (int i = 0; i < monk.Level; i++)
        {
            CreatureLevelInfo levelInfo = monk.LevelInfo[i];
            int currentCharacterLevel = i + 1;

            if (levelInfo.ClassInfo.Class == ObsoletePoeClass && firstPoeLevel == 0)
            {
                firstPoeLevel = currentCharacterLevel;
                RelevelToLastLevelBeforePoe(monk, eventData.Player, currentCharacterLevel);
            }

            if (levelInfo.ClassInfo.Class != NwClass.FromClassType(ClassType.Monk)) continue;

            monkLevelCounter++;

            CheckStunningFistRemoval(levelInfo, monkLevelCounter, eventData.Player, monk);
            CheckImpKnockdownRemoval(levelInfo, monkLevelCounter, eventData.Player, monk);
            CheckCircleKickReplacement(levelInfo, currentCharacterLevel, eventData.Player, monk);

            ValidateFeats(levelInfo, monkLevelCounter, currentCharacterLevel, monk, additionReport);
        }

        if (removalReport.Count > 0)
        {
            const string messageBase = "[Monk Validator] The following feats were removed:\n";
            string removedFeatsString = string.Join("\n", removalReport).ColorString(ColorConstants.Red);
            eventData.Player.SendServerMessage($"{messageBase}{removedFeatsString}");
        }

        if (additionReport.Count > 0)
        {
            const string messageBase = "[Monk Validator] The following feats were updated/added:\n";
            string addedFeatsString = string.Join("\n", additionReport).ColorString(ColorConstants.Cyan);
            eventData.Player.SendServerMessage($"{messageBase}{addedFeatsString}");
        }
    }

    private static void RelevelToLastLevelBeforePoe(NwCreature monk, NwPlayer player, int currentCharacterLevel)
    {
        int targetLevel = currentCharacterLevel - 1;
        int targetXp = targetLevel * (targetLevel - 1) * 500;
        int originalXp = monk.Xp;

        monk.Xp = targetXp;
        monk.Xp = originalXp;

        player.FloatingTextString("Obsolete Path of Enlightenment prestige class found and removed!",
            false);
    }

    private static void PurgeInvalidFeats(NwCreature monk, int totalMonkLevel, List<string> removalReport)
    {
        foreach (NwFeat feat in monk.Feats)
        {
            if (OldFeats.Contains(feat))
            {
                monk.RemoveFeat(feat, true);
                removalReport.Add($" -{feat.Name} (Outdated)");
                continue;
            }

            var requirement = MonkFeatsByLevel.FirstOrDefault(kvp => kvp.Value == feat);

            if (requirement.Value != null && requirement.Key > totalMonkLevel)
            {
                monk.RemoveFeat(feat, true);
                removalReport.Add($" -{feat.Name} (Required Monk Lv {requirement.Key})");
            }
        }
    }

    private static void CheckCircleKickReplacement(CreatureLevelInfo levelInfo, int currentCharacterLevel, NwPlayer player, NwCreature monk)
    {
        if (NwFeat.FromFeatType(Feat.CircleKick) is not { } originalCircleKick
            || NwFeat.FromFeatId(MonkFeat.CircleKickNew) is not { } newCircleKick
            || !levelInfo.Feats.Contains(originalCircleKick))
            return;

        monk.RemoveFeat(originalCircleKick, true);

        if (!monk.KnowsFeat(newCircleKick))
            monk.AddFeat(newCircleKick, currentCharacterLevel);

        player.SendServerMessage("[Monk Validator] Replaced old Circle Kick with a toggleable version.");
    }

    private static void CheckStunningFistRemoval(CreatureLevelInfo levelInfo, int currentMonkLevel, NwPlayer player,
        NwCreature monk)
    {
        if (currentMonkLevel != 1
            || NwFeat.FromFeatType(Feat.StunningFist) is not { } stunningFist
            || !levelInfo.Feats.Contains(stunningFist))
            return;

        monk.RemoveFeat(stunningFist, removeFeatFromLevelList: true);

        player.SendServerMessage(
            $"[Monk Validator] Stunning Fist removed as a free feat at Monk Level {currentMonkLevel}. " +
            $"You may reselect this feat as a general feat.");
    }

    private void CheckImpKnockdownRemoval(CreatureLevelInfo levelInfo, int currentMonkLevel, NwPlayer player, NwCreature monk)
    {
        if (currentMonkLevel != 6
            || levelInfo.Feats.Any(f => f.Id == MonkFeat.MonkFightingStyle)
            || NwFeat.FromFeatType(Feat.ImprovedKnockdown) is not { } impKnockdown
            || NwFeat.FromFeatType(Feat.Knockdown) is not { } knockdown
            || !levelInfo.Feats.Contains(knockdown))
            return;

        monk.RemoveFeat(knockdown, removeFeatFromLevelList: true);
        monk.RemoveFeat(impKnockdown, removeFeatFromLevelList: true);

        player.SendServerMessage(
            $"[Monk Validator] Knockdown and Improved Knockdown removed as free feats at Monk Level {currentMonkLevel}. " +
            $"You may re-select these feats through the Monk Fighting Style feat in your class radial.");
    }


    private static void ValidateFeats(CreatureLevelInfo levelInfo, int currentMonkLevel, int currentCharacterLevel,
        NwCreature monk, List<string> additionReport)
    {
        if (!MonkFeatsByLevel.TryGetValue(currentMonkLevel, out NwFeat? requiredFeat) || requiredFeat == null) return;

        if (levelInfo.Feats.Contains(requiredFeat)) return;

        if (monk.KnowsFeat(requiredFeat))
        {
            monk.RemoveFeat(requiredFeat, true);
        }

        monk.AddFeat(requiredFeat, currentCharacterLevel);

        additionReport.Add($" -{requiredFeat.Name}");
    }
}
