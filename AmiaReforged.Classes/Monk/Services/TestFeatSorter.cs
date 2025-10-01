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
        HashSet<NwFeat?> oldFeats =
        [
            NwFeat.FromFeatType(Feat.StunningFist),
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
                monk.RemoveFeat(feat);
                player.SendServerMessage($"Feat {feat.Name} was removed.");
        }

        // Add new feats that monk qualifies for but doesn't yet have
        foreach (NwFeat feat in correctFeats.Where(feat => !monk.KnowsFeat(feat)))
        {
            monk.AddFeat(feat);
            player.SendServerMessage($"Feat {feat.Name} was added.");
        }
    }
}
