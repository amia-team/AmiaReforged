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
        if (castData.TargetObject != null)
        {
            DevourSingleTarget(warlock, castData.TargetObject, invocationCl);
        }
        else if (castData.TargetLocation != null)
        {
            DevourArea(castData.TargetLocation, warlock, invocationCl);
        }
    }

    private void DevourSingleTarget(NwCreature warlock, NwGameObject targetObject, int invocationCl)
    {
        if (dispelService.IsDispelImmune(targetObject)) return;

        int dispelCount = dispelService.DispelEffectsAll(warlock, targetObject, invocationCl, DispelService.DispelType.DevourMagic);
        targetObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDestruction));

        if (dispelCount <= 0) return;

        _ = Heal(warlock, dispelCount);
    }

    private void DevourArea(Location location, NwCreature warlock, int invocationCl)
    {
        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfMysticalExplosion, fScale: 0.2f));

        HashSet<int> dispelledSpellIds = [];
        foreach (NwGameObject targetObject in location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large,
                     losCheck: true, ObjectTypes.AreaOfEffect | ObjectTypes.Creature | ObjectTypes.Placeable))
        {
            if (targetObject is NwAreaOfEffect aoeObject)
            {
                targetObject.Location?.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDestruction));
                if (dispelService.TryDispelAreaOfEffect(warlock, aoeObject, invocationCl) && aoeObject.Spell != null)
                        dispelledSpellIds.Add(aoeObject.Spell.Id);
                continue;
            }

            if (dispelService.IsDispelImmune(targetObject)) continue;

            if (dispelService.DispelEffectsAll(warlock, targetObject, casterLevel: invocationCl,
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
