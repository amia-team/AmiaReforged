using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(TestFeatSorter))]
public class TestFeatSorter
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly Dictionary<int, List<NwFeat?>> MonkFeatsByLevel = new()
    {
        [2] = [NwFeat.FromFeatId(MonkFeat.StunningStrike)],
        [3] = [NwFeat.FromFeatId(MonkFeat.MonkDefense)],
        [4] = [NwFeat.FromFeatId(MonkFeat.MonkSpeedNew)],
        [5] = [NwFeat.FromFeatId(MonkFeat.EagleStrike)],
        [6] = [NwFeat.FromFeatId(MonkFeat.MonkFightingStyle)],
        [7] = [NwFeat.FromFeatId(MonkFeat.WholenessOfBodyNew)],
        [11] = [NwFeat.FromFeatId(MonkFeat.KiBarrier)],
        [12] = [NwFeat.FromFeatId(MonkFeat.PoeBase)],
        [13] = [NwFeat.FromFeatType(Feat.DiamondSoul)],
        [14] = [NwFeat.FromFeatId(MonkFeat.AxiomaticStrike)],
        [15] = [NwFeat.FromFeatId(MonkFeat.EmptyBodyNew)],
        [16] = [NwFeat.FromFeatId(MonkFeat.QuiveringPalmNew)],
        [17] = [NwFeat.FromFeatId(MonkFeat.KiShout)],
        [18] = [NwFeat.FromFeatId(MonkFeat.KiStrike)],
        [24] = [NwFeat.FromFeatId(MonkFeat.KiStrike2)],
        [30] = [NwFeat.FromFeatId(MonkFeat.KiStrike3)]
    };

    public TestFeatSorter(EventService eventService)
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;

        eventService.SubscribeAll<OnLevelUp, OnLevelUp.Factory>(OnLevelUpAdjustFeats, EventCallbackType.After);
        eventService.SubscribeAll<OnLevelDown, OnLevelDown.Factory>(OnLevelDownAdjustFeats, EventCallbackType.After);

        Log.Info(message: "Monk Test Leveler Service initialized.");
    }

    private void OnLevelDownAdjustFeats(OnLevelDown eventData)
    {
        if (!eventData.Creature.IsLoginPlayerCharacter(out NwPlayer? player)) return;
        if (!eventData.Creature.Name.Contains("testmonk")) return;
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;

        AdjustFeats(eventData.Creature, player);
    }

    private void OnLevelUpAdjustFeats(OnLevelUp eventData)
    {
        if (!eventData.Creature.IsLoginPlayerCharacter(out NwPlayer? player)) return;
        if (!eventData.Creature.Name.Contains("testmonk")) return;

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;


        AdjustFeats(eventData.Creature, player);
    }

    private void AdjustFeats(NwCreature monk, NwPlayer player)
    {
        HashSet<NwFeat?> oldFeats =
        [
            NwFeat.FromFeatType(Feat.MonkEndurance),
            NwFeat.FromFeatType(Feat.MonkAcBonus),
            NwFeat.FromFeatType(Feat.WholenessOfBody),
            NwFeat.FromFeatType(Feat.EmptyBody),
            NwFeat.FromFeatType(Feat.QuiveringPalm),
            NwFeat.FromFeatId(MonkFeat.KiStrike),
            NwFeat.FromFeatId(MonkFeat.KiStrike2),
            NwFeat.FromFeatId(MonkFeat.KiStrike3)
        ];

        int monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        HashSet<NwFeat> correctFeats = MonkFeatsByLevel.Where(kvp => kvp.Key <= monkLevel)
            .SelectMany(kvp => kvp.Value)
            .OfType<NwFeat>()
            .ToHashSet();

        HashSet<NwFeat> incorrectFeats = MonkFeatsByLevel.Where(kvp => kvp.Key > monkLevel)
            .SelectMany(kvp => kvp.Value)
            .OfType<NwFeat>()
            .ToHashSet();

        // Remove old feats or new feats that monk doesn't qualify for
        foreach (NwFeat feat in monk.Feats.Where(feat => oldFeats.Contains(feat) || incorrectFeats.Contains(feat)))
        {
                monk.RemoveFeat(feat, true);
                player.SendServerMessage($"Feat {feat.Name} was removed.");
        }

        // Add new feats that monk qualifies for but doesn't yet have
        foreach (NwFeat feat in correctFeats.Where(feat => !monk.KnowsFeat(feat)))
        {
            monk.AddFeat(feat, monk.Level);
            player.SendServerMessage($"Feat {feat.Name} was added.");
        }

        switch (monkLevel)
        {
            // remove Stunning Fist only on level 2 monk so it can be taken again later
            case 2:
            {
                foreach (NwFeat feat in monk.Feats)
                {
                    if (feat.FeatType is not Feat.StunningFist) continue;
                    monk.RemoveFeat(feat, true);
                    player.SendServerMessage($"Feat {feat.Name} was removed.");
                }

                break;
            }
            // remove KD/IKD for Fighting Style selection
            case 6:
            {
                foreach (NwFeat feat in monk.Feats)
                {
                    if (feat.FeatType is not (Feat.Knockdown or Feat.ImprovedKnockdown)) continue;
                    monk.RemoveFeat(feat, true);
                    player.SendServerMessage($"Feat {feat.Name} was removed.");
                }

                break;
            }
            case 12:
            {
                foreach (NwFeat feat in monk.Feats)
                {
                    if (feat.FeatType is not Feat.DiamondSoul) continue;
                    monk.RemoveFeat(feat, true);
                    player.SendServerMessage($"Feat {feat.Name} was removed.");
                }

                break;
            }
        }




    }
}
