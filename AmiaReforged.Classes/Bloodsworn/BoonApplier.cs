using AmiaReforged.Classes.Bloodsworn.Boons;
using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Bloodsworn;

[ServiceBinding(typeof(BoonApplier))]
public class BoonApplier
{
    private const string BoonTag = "bloodsworn_boon";

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly BoonFactory _boonFactory;

    public BoonApplier(BoonFactory boonFactory)
    {
        _boonFactory = boonFactory;

        NwModule.Instance.OnClientEnter += ApplyOnEnter;
        NwModule.Instance.OnPlayerLevelUp += ApplyOnLevelUp;
        Log.Info("Bloodsworn Boon Applier initialized.");
    }

    private void ApplyOnEnter(ModuleEvents.OnClientEnter eventData)
    {
        if (eventData.Player.ControlledCreature is not { } creature
            || creature.BloodswornLevel() <= 0)
            return;

        RefreshBoons(creature, creature.BloodswornLevel());
    }

    private void ApplyOnLevelUp(ModuleEvents.OnPlayerLevelUp eventData)
    {
        if (eventData.Player.ControlledCreature is not { } creature
            || creature.BloodswornLevel() <= 0)
            return;

        RefreshBoons(creature, creature.BloodswornLevel());
    }

    private void RefreshBoons(NwCreature creature, int bloodswornLevel)
    {
        Effect? linkedBoons = creature.ActiveEffects.FirstOrDefault(e => e.Tag == BoonTag);
        if (linkedBoons != null)
            creature.RemoveEffect(linkedBoons);

        List<(Effect, string)> boonList = [];

        foreach (BoonType boonType in Enum.GetValues(typeof(BoonType)))
        {
            NwFeat? boonFeat = NwFeat.FromFeatId((int)boonType);
            if (boonFeat == null)
            {
                Log.Warn("Bloodsworn boon feat id {FeatId} for {BoonType} could not be resolved.", (int)boonType, boonType);
                continue;
            }

            if (!creature.KnowsFeat(boonFeat))
                continue;

            IBoon? boon = _boonFactory.GetBoon(boonType);
            if (boon == null)
            {
                creature.ControllingPlayer?.SendServerMessage($"Could not find boon for {boonType}");
                continue;
            }

            boonList.Add((boon.GetBoonEffect(bloodswornLevel), boon.GetBoonMessage(bloodswornLevel)));
        }

        if (boonList.Count == 0) return;

        Color darkRed = new(150, 0, 0);
        string message = "Your Bloodsworn Master grants you the following boons:".ColorString(darkRed);
        linkedBoons = Effect.VisualEffect(VfxType.None);

        Color lightRed = new(150, 50, 50);
        foreach ((Effect Effect, string Message) boon in boonList)
        {
            linkedBoons = Effect.LinkEffects(linkedBoons, boon.Effect);
            message += $"\n - {boon.Message}".ColorString(lightRed);
        }

        linkedBoons.SubType = EffectSubType.Unyielding;
        linkedBoons.Tag = BoonTag;

        creature.ApplyEffect(EffectDuration.Permanent, linkedBoons);

        creature.ControllingPlayer?.SendServerMessage(message);
        creature.ControllingPlayer?.ApplyInstantVisualEffectToObject(VfxType.ImpGoodHelp, creature);
        creature.ControllingPlayer?.ApplyInstantVisualEffectToObject(VfxType.ImpEvilHelp, creature);
    }
}
