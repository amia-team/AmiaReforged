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
        int dispelModifier = dispelService.GetDispelModifier(warlock, invocationCl, castData.Spell);
        Effect impVfx = Effect.VisualEffect(VfxType.ImpDestruction);

        if (castData.TargetObject != null)
        {
            DevourTarget(warlock, castData.TargetObject, dispelModifier, castData.Spell, impVfx);
        }
        else if (castData.TargetLocation != null)
        {
            DevourArea(castData.TargetLocation, warlock, dispelModifier, castData.Spell, impVfx);
        }
    }

    private void DevourTarget(NwCreature warlock, NwGameObject target, int dispelModifier, NwSpell spell, Effect impVfx)
    {
        dispelService.SignalDispel(warlock, target, spell);
        if (dispelService.IsImmuneToDispel(target)) return;

        Effect dispelMagic = dispelService.DispelMagic(dispelModifier, caster: warlock);
        target.ApplyEffect(EffectDuration.Instant, dispelMagic);
        target.ApplyEffect(EffectDuration.Instant, impVfx);

        int dispelCount = dispelService.FlushDispelFeedback(warlock);
        if (dispelCount == 0) return;
        _ = Heal(warlock, dispelCount);
    }

    private void DevourArea(Location location, NwCreature warlock, int dispelModifier, NwSpell spell, Effect impVfx)
    {
        Effect dispelMagic = dispelService.DispelMagic(dispelModifier, warlock, maxSpells: 1);

        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfMysticalExplosion, fScale: 0.2f));

        foreach (NwGameObject targetObject in location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large,
                     losCheck: true, ObjectTypes.AreaOfEffect | ObjectTypes.Creature | ObjectTypes.Placeable))
        {
            dispelService.SignalDispel(warlock, targetObject, spell);

            if (targetObject is NwCreature creature && warlock.IsReactionTypeFriendly(creature)
                || dispelService.IsImmuneToDispel(targetObject)) continue;

            targetObject.ApplyEffect(EffectDuration.Instant, dispelMagic);
            targetObject.ApplyEffect(EffectDuration.Instant, impVfx);
        }

        int dispelCount = dispelService.FlushDispelFeedback(warlock);
        if (dispelCount == 0) return;
        _ = Heal(warlock, dispelCount);
    }

    private static async Task Heal(NwCreature warlock, int dispelCount)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(1));
        warlock.ApplyEffect(EffectDuration.Instant, Effect.Heal(dispelCount * 5));
        warlock.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHealingM));
    }
}
