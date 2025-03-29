using System.Diagnostics.Tracing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.Classes.Spells.Arcane.FourthCircle.Illusion.ShadowConjuration;

namespace AmiaReforged.Classes.Spells.Arcane.FirstCircle.Evocation;

[ServiceBinding(typeof(ISpell))]
public class MagicMissile : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }


    public string ImpactScript => "NW_S0_MagMiss";

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature casterCreature) return;

        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        SpellUtils.SignalSpell(casterCreature, target, eventData.Spell);
        
        if (ResistedSpell) return;

        switch (eventData.Spell.SpellType)
        {
            case Spell.MagicMissile:
                _ = DoMagicMissile();
                break;
            case Spell.ShadowConjurationMagicMissile:
                _ = DoShadowMagicMissile();
                break;
            default: return;
        }

        return;

        async Task DoMagicMissile()
        {
            float distanceToTarget = casterCreature.Distance(target);
            float missileTravelDelay = distanceToTarget / (3f * float.Log(distanceToTarget) + 2f);
        
            int numberOfMissiles = casterCreature.CasterLevel switch
            {
                3 or 4 => 2,
                5 or 6 => 3,
                7 or 8 => 4,
                >= 9 => 5,
                _ => 1
            };

            Effect missileProjectileVfx = Effect.VisualEffect(VfxType.ImpMirv);
        
            for (int i = 0; i < numberOfMissiles; i++)
            {
                target.ApplyEffect(EffectDuration.Instant, missileProjectileVfx);
                await NwTask.Delay(TimeSpan.FromSeconds(0.1f));
            }
        
            await NwTask.Delay(TimeSpan.FromSeconds(missileTravelDelay));

            for (int i = 0; i < numberOfMissiles; i++)
            {
                ApplyMissileEffect(casterCreature, target, eventData.MetaMagicFeat);
                await NwTask.Delay(TimeSpan.FromSeconds(0.1f));
            }

            bool hasEpicFocus = casterCreature.KnowsFeat(Feat.EpicSpellFocusEvocation!);
        
            if (!hasEpicFocus) return;

            NwGameObject? firstHostile = target.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, true).
                FirstOrDefault(o => 
                    o is NwCreature creature 
                    && creature.IsReactionTypeHostile(casterCreature)
                    && creature != target);

            if (firstHostile is not NwCreature firstHostileCreature) return;

            await NwTask.Delay(TimeSpan.FromSeconds(0.1f) * (numberOfMissiles + 1));
        
            for (int i = 0; i < numberOfMissiles; i++)
            {
                firstHostileCreature.ApplyEffect(EffectDuration.Instant, missileProjectileVfx);
                await NwTask.Delay(TimeSpan.FromSeconds(0.1f));
            }
        
            for (int i = 0; i < numberOfMissiles; i++)
            {
                ApplyMissileEffect(casterCreature, firstHostileCreature, eventData.MetaMagicFeat);
                await NwTask.Delay(TimeSpan.FromSeconds(0.1f));
            }
        }
        
        async Task DoShadowMagicMissile()
        {
            float distanceToTarget = casterCreature.Distance(target);
            float missileTravelDelay = distanceToTarget / (3f * float.Log(distanceToTarget) + 2f);
        
            int numberOfMissiles = casterCreature.CasterLevel switch
            {
                3 or 4 => 2,
                5 or 6 => 3,
                7 or 8 => 4,
                >= 9 => 5,
                _ => 1
            };

            Effect missileProjectileVfx = Effect.VisualEffect(VfxType.ImpMirv);
        
            for (int i = 0; i < numberOfMissiles; i++)
            {
                target.ApplyEffect(EffectDuration.Instant, missileProjectileVfx);
                await NwTask.Delay(TimeSpan.FromSeconds(0.1f));
            }
        
            await NwTask.Delay(TimeSpan.FromSeconds(missileTravelDelay));

            for (int i = 0; i < numberOfMissiles; i++)
            {
                ApplyShadowMissileEffect(casterCreature, target, eventData.MetaMagicFeat);
                await NwTask.Delay(TimeSpan.FromSeconds(0.1f));
            }

            bool hasEpicFocus = casterCreature.KnowsFeat(Feat.EpicSpellFocusIllusion!);
        
            if (!hasEpicFocus) return;

            NwGameObject? firstHostile = target.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, true).
                FirstOrDefault(o => 
                    o is NwCreature creature 
                    && creature.IsReactionTypeHostile(casterCreature)
                    && creature != target);

            if (firstHostile is not NwCreature firstHostileCreature) return;

            await NwTask.Delay(TimeSpan.FromSeconds(0.1f) * (numberOfMissiles + 1));
        
            for (int i = 0; i < numberOfMissiles; i++)
            {
                firstHostileCreature.ApplyEffect(EffectDuration.Instant, missileProjectileVfx);
                await NwTask.Delay(TimeSpan.FromSeconds(0.1f));
            }
        
            for (int i = 0; i < numberOfMissiles; i++)
            {
                ApplyShadowMissileEffect(casterCreature, firstHostileCreature, eventData.MetaMagicFeat);
                await NwTask.Delay(TimeSpan.FromSeconds(0.1f));
            }
        }
    }
    
    private static void ApplyMissileEffect(NwCreature casterCreature, NwGameObject target, MetaMagic metaMagic)
    {
        int damage = CalculateMissileDamage(casterCreature, metaMagic);
            
        Effect damageEffect = Effect.LinkEffects(Effect.Damage(damage), 
            Effect.VisualEffect(VfxType.ImpMagblue));
            
        target.ApplyEffect(EffectDuration.Instant, damageEffect);
        
    }

    private static int CalculateMissileDamage(NwCreature casterCreature, MetaMagic metaMagic)
    {
        bool hasFocus = casterCreature.KnowsFeat(Feat.SpellFocusEvocation!);
        bool hasGreaterFocus = casterCreature.KnowsFeat(Feat.GreaterSpellFocusEvocation!);
        bool hasEpicFocus = casterCreature.KnowsFeat(Feat.EpicSpellFocusEvocation!);

        int damage = SpellUtils.CheckMaximize(metaMagic,4, 1) + 1;
        damage = hasFocus ? damage + 1 : hasGreaterFocus ? damage + 2 : hasEpicFocus ? damage + 3 : damage;
        damage = SpellUtils.CheckEmpower(metaMagic, damage);

        return damage;
    }
    
    private static void ApplyShadowMissileEffect(NwCreature casterCreature, NwGameObject target, MetaMagic metaMagic)
    {
        int damage = CalculateShadowDamage(casterCreature, metaMagic);
            
        Effect damageEffect = Effect.LinkEffects(
            Effect.Damage(damage, DamageType.Cold), 
            Effect.Damage(damage, DamageType.Negative),
            Effect.VisualEffect(VfxType.ImpFrostS),
            Effect.VisualEffect(VfxType.ComHitNegative));
            
        target.ApplyEffect(EffectDuration.Instant, damageEffect);
        
    }

    private static int CalculateShadowDamage(NwCreature casterCreature, MetaMagic metaMagic)
    {
        bool hasFocus = casterCreature.KnowsFeat(Feat.SpellFocusIllusion!);
        bool hasGreaterFocus = casterCreature.KnowsFeat(Feat.GreaterSpellFocusIllusion!);
        bool hasEpicFocus = casterCreature.KnowsFeat(Feat.EpicSpellFocusIllusion!);

        int damage = SpellUtils.CheckMaximize(metaMagic,4, 1) + 1;
        damage = hasFocus ? damage + 1 : hasGreaterFocus ? damage + 2 : hasEpicFocus ? damage + 3 : damage;
        damage = SpellUtils.CheckEmpower(metaMagic, damage);

        return damage;
    }

    
}