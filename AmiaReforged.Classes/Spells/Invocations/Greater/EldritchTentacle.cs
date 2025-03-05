using AmiaReforged.Classes.Types;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public static class EldritchTentacle
{
    public static void StrikeTargetWithTentacle(uint target, uint caster)
    {
        if (target == caster) return;
        
        int nTentacles = d4();
        int nHits;
        int targetOpposeCheck = GetBaseAttackBonus(target) + GetAbilityModifier(ABILITY_STRENGTH, target);
        int targetSize = GetCreatureSize(target);


        // Making creature size count for grapple checks.
        if (targetSize == CREATURE_SIZE_TINY)
        {
            targetOpposeCheck += -8;
        }

        if (targetSize == CREATURE_SIZE_SMALL)
        {
            targetOpposeCheck += -4;
        }

        if (targetSize == CREATURE_SIZE_LARGE)
        {
            targetOpposeCheck += 4;
        }

        if (targetSize == CREATURE_SIZE_HUGE)
        {
            targetOpposeCheck += 8;
        }

        // Setting up the check for the tentacles.
        int tentacleHitCheck = d20() + GetCasterLevel(caster) + 8;

        // Rolling d4 tentacles against each target.
        for (nHits = nTentacles; nHits > 0; nHits--)
        {
            bool hit = targetOpposeCheck < tentacleHitCheck;
            if (!hit) return;

            ApplyEffectToObject(DURATION_TYPE_INSTANT,
                EffectDamage(d6() + 4, DAMAGE_TYPE_BLUDGEONING), target);

            if (FortitudeSave(target, WarlockConstants.CalculateDC(caster), SAVING_THROW_TYPE_SPELL, caster) == TRUE) return;
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectParalyze(), target, RoundsToSeconds(1));
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_PARALYZED), target,
                RoundsToSeconds(1));
        }
    }
}