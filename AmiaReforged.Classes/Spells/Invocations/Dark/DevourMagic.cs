using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

[ServiceBinding(typeof(IInvocation))]
public class DevourMagic(DispelService dispelService) : IInvocation
{
    public string ImpactScript => "wlk_devourmagic";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        int dispelModifier = dispelService.GetDispelModifier(warlock, invocationCl);

        if (castData.TargetObject != null)
        {
            DevourSingleTarget(warlock, castData.TargetObject, dispelModifier);
        }
        else if (castData.TargetLocation != null)
        {
            DevourArea(castData.TargetLocation, warlock, dispelModifier);
        }
    }

    private void DevourSingleTarget(NwCreature warlock, NwGameObject targetObject, int dispelModifier)
    {
        if (dispelService.IsImmuneToDispel(targetObject)) return;

        int dispelCount = dispelService.DispelTarget(warlock, targetObject, dispelModifier, DispelService.DispelType.DevourMagic);
        targetObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDestruction));

        if (dispelCount <= 0) return;

        _ = Heal(warlock, dispelCount);
    }

    private void DevourArea(Location location, NwCreature warlock, int dispelModifier)
    {
        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfMysticalExplosion, fScale: 0.2f));

        HashSet<int> dispelledSpellIds = [];
        foreach (NwGameObject targetObject in location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large,
                     losCheck: true, ObjectTypes.AreaOfEffect | ObjectTypes.Creature | ObjectTypes.Placeable))
        {
            if (targetObject is NwAreaOfEffect aoeObject)
            {
                targetObject.Location?.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDestruction));
                if (dispelService.TryDispelAreaOfEffect(warlock, aoeObject, dispelModifier) && aoeObject.Spell != null)
                        dispelledSpellIds.Add(aoeObject.Spell.Id);
                continue;
            }

            if (dispelService.IsImmuneToDispel(targetObject)) continue;

            if (dispelService.DispelTarget(warlock, targetObject, dispelModifier,
                    DispelService.DispelType.DevourMagic, maxSpells: 1) <= 0) continue;

            targetObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDestruction));
        }

        if (dispelledSpellIds.Count <= 0) return;

        _ = Heal(warlock, dispelledSpellIds.Count);
    }

    private static async Task Heal(NwCreature warlock, int dispelCount)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(1));
        warlock.ApplyEffect(EffectDuration.Instant, Effect.Heal(dispelCount * 5));
        warlock.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHealingM));
    }
}
