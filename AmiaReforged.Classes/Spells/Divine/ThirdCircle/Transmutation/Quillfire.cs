using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.ThirdCircle.Transmutation;

[ServiceBinding(typeof(ISpell))]
public class Quillfire : ISpell
{
    public string ImpactScript => "x0_s0_quillfire";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        if (eventData.TargetObject is not NwCreature targetCreature) return;

        if (caster.IsReactionTypeFriendly(targetCreature)) return;

        SpellUtils.SignalSpell(caster, targetCreature, eventData.Spell);

        int numberOfQuills = caster.CasterLevel / 5;
        int dc = SpellUtils.GetSpellDc(eventData);

        float distanceToTarget = caster.Distance(targetCreature);
        float quillTravelDelay = distanceToTarget / (3f * float.Log(distanceToTarget) + 2f);

        const VfxType quillProjectile = (VfxType)359;
        Effect quillProjectileVfx = Effect.VisualEffect(quillProjectile);

        for (int i = 0; i < numberOfQuills; i++)
            targetCreature.ApplyEffect(EffectDuration.Instant, quillProjectileVfx);

        _ = FireQuills();
        return;

        async Task FireQuills()
        {
            await NwTask.Delay(TimeSpan.FromSeconds(quillTravelDelay));
            for (int i = 0; i < numberOfQuills; i++)
            {
                bool hasImpEvasion = targetCreature.KnowsFeat(Feat.ImprovedEvasion!);
                bool hasEvasion = targetCreature.KnowsFeat(Feat.Evasion!);

                SavingThrowResult savingThrowDamage =
                    targetCreature.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Spell, caster);

                if (savingThrowDamage == SavingThrowResult.Success)
                    targetCreature.ApplyEffect(EffectDuration.Instant,
                        Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));

                if ((hasEvasion || hasImpEvasion) && savingThrowDamage == SavingThrowResult.Success)
                    continue;

                int quillDamage = CalculateQuillDamage(eventData.MetaMagicFeat);

                if (hasImpEvasion || savingThrowDamage == SavingThrowResult.Success) quillDamage /= 2;

                Effect quillDamageEffect = Effect.LinkEffects(Effect.VisualEffect(VfxType.ComBloodSparkSmall),
                    Effect.Damage(quillDamage, DamageType.Piercing));

                targetCreature.ApplyEffect(EffectDuration.Instant, quillDamageEffect);
                targetCreature.ApplyEffect(EffectDuration.Permanent, Effect.Poison(PoisonType.LargeScorpionVenom));
            }
        }
    }

    private static int CalculateQuillDamage(MetaMagic metaMagic)
    {
        int quillDamage = SpellUtils.MaximizeSpell(metaMagic, 6, 4);
        quillDamage = SpellUtils.EmpowerSpell(metaMagic, quillDamage);

        return quillDamage;
    }

    // This spell doesn't use Spell Resistance
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }

    public void SetSpellResisted(bool result) { }

}
