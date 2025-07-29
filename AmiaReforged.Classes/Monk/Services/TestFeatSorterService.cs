using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(TestFeatSorterService))]
public class TestFeatSorterService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly Dictionary<int, List<NwFeat?>> MonkFeatProgression = new()
    {
        [1] = [NwFeat.FromFeatId(MonkFeat.StunningStrike), NwFeat.FromFeatType(Feat.WeaponProficiencySimple)],
        [3] = [NwFeat.FromFeatId(MonkFeat.MonkDefense)],
        [4] = [NwFeat.FromFeatId(MonkFeat.MonkSpeedNew), NwFeat.FromFeatId(MonkFeat.EagleStrike)],
        [6] = [NwFeat.FromFeatId(MonkFeat.MonkFightingStyle)],
        [7] = [NwFeat.FromFeatId(MonkFeat.WholenessOfBodyNew), NwFeat.FromFeatId(MonkFeat.BodyKiPoint1)],
        [11] = [NwFeat.FromFeatId(MonkFeat.AxiomaticStrike), NwFeat.FromFeatId(MonkFeat.BodyKiPoint2)],
        [12] = [NwFeat.FromFeatId(MonkFeat.PoeBase)],
        [14] = [NwFeat.FromFeatId(MonkFeat.KiBarrier)],
        [15] = [NwFeat.FromFeatId(MonkFeat.EmptyBodyNew), NwFeat.FromFeatId(MonkFeat.BodyKiPoint3)],
        [17] = [NwFeat.FromFeatId(MonkFeat.QuiveringPalmNew), NwFeat.FromFeatId(MonkFeat.SpiritKiPoint1)],
        [18] = [NwFeat.FromFeatId(MonkFeat.KiStrike)],
        [19] = [NwFeat.FromFeatId(MonkFeat.KiShout), NwFeat.FromFeatId(MonkFeat.BodyKiPoint4)],
        [22] = [NwFeat.FromFeatId(MonkFeat.SpiritKiPoint2)],
        [23] = [NwFeat.FromFeatId(MonkFeat.BodyKiPoint5)],
        [24] = [NwFeat.FromFeatId(MonkFeat.KiStrike2)],
        [27] = [NwFeat.FromFeatId(MonkFeat.SpiritKiPoint3), NwFeat.FromFeatId(MonkFeat.BodyKiPoint6)],
        [30] = [NwFeat.FromFeatId(MonkFeat.KiStrike3)]
    };

    public TestFeatSorterService(EventService eventService)
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
        if (!eventData.Creature.Name.Contains("test", StringComparison.CurrentCultureIgnoreCase)) return;
        if (!eventData.Creature.Name.Contains("monk", StringComparison.CurrentCultureIgnoreCase)) return;

        AdjustFeats(eventData.Creature, player);
    }

    private void OnLevelUpAdjustFeats(OnLevelUp eventData)
    {
        if (!eventData.Creature.IsLoginPlayerCharacter(out NwPlayer? player)) return;
        if (!eventData.Creature.Name.Contains("test", StringComparison.CurrentCultureIgnoreCase)) return;
        if (!eventData.Creature.Name.Contains("monk", StringComparison.CurrentCultureIgnoreCase)) return;

        AdjustFeats(eventData.Creature, player);
    }

    private void AdjustFeats(NwCreature monk, NwPlayer player)
    {
        foreach (NwFeat feat in monk.Feats)
        {
            if (feat.FeatType is not (Feat.StunningFist or Feat.MonkEndurance or Feat.MonkAcBonus
                    or Feat.WholenessOfBody or Feat.EmptyBody or Feat.QuiveringPalm or Feat.KiStrike)
                && feat.Id is not (MonkFeat.KiStrike or MonkFeat.KiStrike2 or MonkFeat.KiStrike3))
                continue;

            monk.RemoveFeat(feat);
            player.SendServerMessage($"Old feat {feat.Name} removed.");
        }

        int monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        foreach (int featLevel in MonkFeatProgression.Keys.Where(featLevel => featLevel > monkLevel))
        {
            List<NwFeat?> monkFeatsByLevel = MonkFeatProgression[featLevel];

            foreach (NwFeat? feat in monkFeatsByLevel.OfType<NwFeat>().Where(monk.KnowsFeat))
            {
                monk.RemoveFeat(feat);
                player.SendServerMessage($"New feat {feat.Name} removed; you are below the required monk level {featLevel}.");
            }
        }

        foreach (int featLevel in MonkFeatProgression.Keys.Where(featLevel => featLevel <= monkLevel))
        {
            List<NwFeat?> monkFeatsByLevel = MonkFeatProgression[featLevel];

            foreach (NwFeat? feat in monkFeatsByLevel.OfType<NwFeat>().Where(feat => !monk.KnowsFeat(feat)))
            {
                monk.AddFeat(feat);
                player.SendServerMessage($"New feat {feat.Name} added on monk level {featLevel}.");
            }
        }
    }
}
