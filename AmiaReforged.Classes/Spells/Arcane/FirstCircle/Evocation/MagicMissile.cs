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
                _ = DoMagicMissile(casterCreature, target, eventData.MetaMagicFeat);
                break;
            case Spell.ShadowConjurationMagicMissile:
                _ = ShadowMagicMissile.DoShadowMagicMissile(casterCreature, target, eventData.MetaMagicFeat);
                break;
            default: return;
        }
        
        // Check for another hostile target if the caster has epic spell focus
        
        bool hasEpicFocus = 
            (eventData.Spell.SpellType == Spell.MagicMissile && casterCreature.KnowsFeat(Feat.EpicSpellFocusEvocation!))
            || (eventData.Spell.SpellType == Spell.ShadowConjurationMagicMissile && casterCreature.KnowsFeat(Feat.EpicSpellFocusIllusion!));
        
        if (!hasEpicFocus) return;

        NwCreature? firstHostile = (NwCreature?)target.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, true).
            FirstOrDefault(o => 
                o is NwCreature creature 
                && creature != target 
                && creature.IsReactionTypeHostile(casterCreature));

        if (firstHostile is null) return;
        
        switch (eventData.Spell.SpellType)
        {
            case Spell.MagicMissile:
                _ = DoMagicMissile(casterCreature, firstHostile, eventData.MetaMagicFeat);
                break;
            case Spell.ShadowConjurationMagicMissile:
                _ = ShadowMagicMissile.DoShadowMagicMissile(casterCreature, firstHostile, eventData.MetaMagicFeat);
                break;
            default: return;
        }
    }

    private static async Task DoMagicMissile(NwCreature casterCreature, NwGameObject target, MetaMagic metaMagic)
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
            
        Effect damageEffect = Effect.LinkEffects(Effect.Damage(damage), 
            Effect.VisualEffect(VfxType.ImpMagblue, false, 0.7f));
            
        target.ApplyEffect(EffectDuration.Instant, damageEffect);
        
    }

    private static int CalculateDamage(NwCreature casterCreature, MetaMagic metaMagic)
    {
        bool hasFocus = casterCreature.KnowsFeat(Feat.SpellFocusEvocation!);
        bool hasGreaterFocus = casterCreature.KnowsFeat(Feat.GreaterSpellFocusEvocation!);
        bool hasEpicFocus = casterCreature.KnowsFeat(Feat.EpicSpellFocusEvocation!);

        int damage = SpellUtils.MaximizeSpell(metaMagic,4, 1) + 1;
        damage = hasFocus ? damage + 1 : hasGreaterFocus ? damage + 2 : hasEpicFocus ? damage + 3 : damage;
        damage = SpellUtils.EmpowerSpell(metaMagic, damage);

        return damage;
    }

    
}