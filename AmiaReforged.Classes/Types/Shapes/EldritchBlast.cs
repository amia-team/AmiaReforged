using AmiaReforged.Classes.Types.EssenceEffects;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.Shapes;

public static class EldritchBlast
{
    public static void CastEldritchBlast(uint targetObject, EssenceVisuals essenceVisuals,
        EssenceEffectApplier effectApplier, int damage)
    {
        int touchAttackRanged = TouchAttackRanged(targetObject);

        if (touchAttackRanged == 0) return;

        ApplyEffectToObject(DURATION_TYPE_INSTANT, essenceVisuals.ImpactVfx, targetObject);
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, essenceVisuals.BeamVfx, targetObject, 1.1f);
        effectApplier.ApplyEffects(damage * touchAttackRanged);
    }
}