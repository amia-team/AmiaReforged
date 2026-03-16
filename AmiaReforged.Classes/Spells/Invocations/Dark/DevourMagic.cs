using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

public class DevourMagic(DispelService dispelService) : IInvocation
{
    public string ImpactScript => "wlk_devourmagic";
    public void CastInvocation(NwCreature warlock, int warlockLevel, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetLocation != null)
        {
            DevourArea(castData.TargetLocation);
        }
        else if (castData.TargetObject != null)
        {
            DevourSingleTarget(warlock, castData.TargetObject, warlockLevel);
        }
    }

    private void DevourArea(Location location)
    {
        int dispelCount = 0;
        foreach (NwGameObject targetObject in location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, true))
        {
            
        }
    }

    private void DevourSingleTarget(NwCreature warlock, NwGameObject targetObject, int warlockLevel)
    {
        if (dispelService.IsDispelImmune(targetObject)) return;

        int dispelCount = dispelService.DispelEffectsAll(warlock, targetObject, warlockLevel, DispelService.DispelType.DevourMagic);
        targetObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDestruction));

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
