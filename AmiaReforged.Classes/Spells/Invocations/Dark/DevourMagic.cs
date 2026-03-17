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
        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfMysticalExplosion));

        int dispelCount = 0;
        foreach (NwGameObject targetObject in location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, true))
        {
            if (dispelService.IsDispelImmune(targetObject)) continue;

            if (targetObject is NwAreaOfEffect aoeObject
                && dispelService.TryDispelAreaOfEffect(warlock, aoeObject, invocationCl))
            {
                dispelCount++;
                aoeObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDestruction));
                continue;
            }

            if (dispelService.DispelEffectsAll(warlock, targetObject, invocationCl,
                    DispelService.DispelType.DevourMagic, maxSpells: 1) <= 0) continue;

            dispelCount++;
            targetObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDestruction));
        }

        if (dispelCount <= 0) return;

        _ = Heal(warlock, dispelCount);
    }

    private static async Task Heal(NwCreature warlock, int dispelCount)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(1));
        warlock.ApplyEffect(EffectDuration.Instant, Effect.Heal(dispelCount * 5));
        warlock.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHealingM));
    }
}
