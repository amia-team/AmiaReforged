using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FloatingLeaf;

[ServiceBinding(typeof(FloatingLeafLeapService))]
public class FloatingLeafLeapService
{
    private const string FloatingEmptyBodyTag = nameof(PathType.FloatingLeaf) + nameof(TechniqueType.EmptyBody);
    private const string WeightlessLeapTag = nameof(PathType.FloatingLeaf) + "Leaping";

    public FloatingLeafLeapService()
    {
        NwModule.Instance.OnUseFeat += InterceptEmptyBodyToLeap;
    }

    private void InterceptEmptyBodyToLeap(OnUseFeat eventData)
    {
        if (eventData.Feat.Id != MonkFeat.EmptyBodyNew
            || MonkUtils.GetMonkPath(eventData.Creature) != PathType.FloatingLeaf
            || !MonkUtils.GetKiFocus(eventData.Creature).HasValue) return;

        if (TryWeightlessLeap(eventData.Creature))
            eventData.PreventFeatUse = true;
    }

    /// <summary>
    /// Does the weightless leap for Floating Leaf monk, intercepts the CastServiceTechnique
    /// </summary>
    /// <param name="monk"></param>
    /// <returns>True if the leap was successful, false if not</returns>
    private static bool TryWeightlessLeap(NwCreature monk)
    {
        bool hasEmptyBody = monk.ActiveEffects.Any(e => e.Tag == FloatingEmptyBodyTag);
        if (!hasEmptyBody || IsNoFlyArea(monk) || IsLeaping(monk)) return false;

        monk.ControllingPlayer?.EnterTargetMode(targetingData => DoLeap(monk, targetingData),
            new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Placeable | ObjectTypes.Creature | ObjectTypes.Door | ObjectTypes.Tile
        });

        return true;
    }

    private static void DoLeap(NwCreature monk, ModuleEvents.OnPlayerTarget targetingData)
    {
        if (monk.Area == null || monk.Location == null) return;

        Location targetLocation = Location.Create(monk.Area, targetingData.TargetPosition, monk.Location.Rotation);

        if (monk.Location.Distance(targetLocation) > 20)
        {
            monk.ControllingPlayer?.FloatingTextString("You can only leap within 20 meters",
                false, false);

            return;
        }
        NwPlaceable? dummy = NwPlaceable.Create("x2_plc_psheet", targetLocation);
        if (dummy == null)
        {
            monk.ControllingPlayer?.SendServerMessage("DEBUG: Dummy creature failed to materialize for " +
                                                      "Floating Leaf leap, please do a bug report!");
            return;
        }
        if (!monk.HasLineOfSight(dummy))
        {
            monk.ControllingPlayer?.SendServerMessage("You don't have line of sight to where you want to leap");
            dummy.Destroy();
            return;
        }
        dummy.Destroy();

        monk.ClearActionQueue();
        monk.ApplyEffect(EffectDuration.Temporary, Effect.CutsceneImmobilize(), TimeSpan.FromSeconds(1.9));
        monk.FaceToPoint(targetLocation.Position);

        _ = DelayedLeap();

        return;

        async Task DelayedLeap()
        {
            await NwTask.Delay(TimeSpan.FromMilliseconds(100));

            Effect leapEffect = Effect.DisappearAppear(targetLocation);
            leapEffect.Tag = WeightlessLeapTag;
            monk.ApplyEffect(EffectDuration.Temporary, leapEffect, TimeSpan.FromSeconds(2));
        }
    }

    private static bool IsNoFlyArea(NwCreature monk)
    {
        if (monk.Area != null && monk.Area.GetObjectVariable<LocalVariableInt>("CS_NO_FLY").Value != 1) return false;

        monk.ControllingPlayer?.FloatingTextString("- You are unable to fly in this area! -",
            false, false);

        return true;
    }

    private static bool IsLeaping(NwCreature monk)
        => monk.ActiveEffects.Any(e => e.Tag == WeightlessLeapTag);
}
