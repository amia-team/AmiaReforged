﻿using Anvil.API;

namespace AmiaReforged.Classes.Spells.Arcane.FourthCircle.Illusion.ShadowConjuration;

public static class ShadowMagicMissile
{
    public static async Task DoShadowMagicMissile(NwCreature casterCreature, NwGameObject target, MetaMagic metaMagic)
    {
        float distanceToTarget = casterCreature.Distance(target);
        float missileTravelDelay = distanceToTarget / (3f * float.Log(distanceToTarget) + 3f);

        int numberOfMissiles = Math.Min(1 + (casterCreature.CasterLevel - 1) / 2, 5);

        Effect missileProjectileVfx = Effect.VisualEffect(VfxType.ImpMirv);
        
        for (int i = 0; i < numberOfMissiles; i++)
        {
            target.ApplyEffect(EffectDuration.Instant, missileProjectileVfx);
            await NwTask.Delay(TimeSpan.FromSeconds(0.1f));
        }
        
        await NwTask.Delay(TimeSpan.FromSeconds(missileTravelDelay));

        for (int i = 0; i < numberOfMissiles; i++)
        {
            await casterCreature.WaitForObjectContext();
            ApplyMissileEffect(casterCreature, target, metaMagic);
            await NwTask.Delay(TimeSpan.FromSeconds(0.1f));
        }
    }

    private static void ApplyMissileEffect(NwCreature casterCreature, NwGameObject target, MetaMagic metaMagic)
    {
        int damage = CalculateDamage(casterCreature, metaMagic);
            
        Effect damageEffect = Effect.LinkEffects(
            Effect.Damage(damage, DamageType.Cold), 
            Effect.Damage(damage, DamageType.Negative),
            Effect.VisualEffect(VfxType.ImpFrostS),
            Effect.VisualEffect(VfxType.ComHitNegative, false, 1.5f));
            
        target.ApplyEffect(EffectDuration.Instant, damageEffect);
        
    }

    private static int CalculateDamage(NwCreature casterCreature, MetaMagic metaMagic)
    {
        bool hasFocus = casterCreature.KnowsFeat(Feat.SpellFocusIllusion!);
        bool hasGreaterFocus = casterCreature.KnowsFeat(Feat.GreaterSpellFocusIllusion!);
        bool hasEpicFocus = casterCreature.KnowsFeat(Feat.EpicSpellFocusIllusion!);

        int damage = SpellUtils.MaximizeSpell(metaMagic,4, 1) + 1;
        damage = hasFocus ? damage + 1 : hasGreaterFocus ? damage + 2 : hasEpicFocus ? damage + 3 : damage;
        damage = SpellUtils.EmpowerSpell(metaMagic, damage);

        return damage;
    }
    
}