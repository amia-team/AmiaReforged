using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.EighthCircle.Transmutation;

/// <summary>
/// Level: Druid 8
/// Area of effect: Single
/// Valid Metamagic: Still, Silent, Empower, Maximize
/// Save: Fortitude Special
/// Spell Resistance: Yes
/// The caster attempts a melee touch attack to utterly destroy the unnatural. If the attack hits,
/// the victim takes 1d8 points of divine damage per caster level, with a fortitude save halving the damage.
/// If the target is undead, construct, outsider, or aberration, they must make a fortitude save or be slain instantly,
/// and they receive full damage on a successful save.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class SylvanWrath : ISpell
{
    private const VfxType ImpDruidClaw = (VfxType)2547;
    public string ImpactScript => "sylvan_wrath";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.TargetObject is not { } targetObject || eventData.Caster is not NwCreature caster)
            return;

        targetObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(ImpDruidClaw));
        SpellUtils.SignalSpell(caster, targetObject, eventData.Spell);

        if (caster.SpellResistanceCheck(targetObject, eventData.Spell, caster.CasterLevel))
            return;

        if (caster.TouchAttackMelee(targetObject) == TouchAttackResult.Miss)
            return;

        _ = DoSylvanWrath(caster, targetObject, eventData.Spell, eventData.SaveDC, eventData.MetaMagicFeat);
    }

    private static async Task DoSylvanWrath(NwCreature caster, NwGameObject targetObject, NwSpell spell, int dc,
        MetaMagic metaMagic)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(1.8));
        await caster.WaitForObjectContext();

        SavingThrowResult savingThrowResult =
            targetObject.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Spell, caster);

        if (savingThrowResult == SavingThrowResult.Success)
            targetObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));

        int damage;

        if (targetObject is NwCreature { Race.RacialType: RacialType.Aberration or RacialType.Construct
                or RacialType.Undead or RacialType.Outsider})
        {
            if (savingThrowResult == SavingThrowResult.Failure)
            {
                Effect death = Effect.Death(true);
                death.IgnoreImmunity = true;
                targetObject.ApplyEffect(EffectDuration.Instant, death);
                targetObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDeathL));
            }
            else
            {
                damage = SpellUtils.EmpowerSpell(metaMagic, 8 * caster.CasterLevel);
                ApplyDamage(damage, targetObject);
            }
            return;
        }

        damage = SpellUtils.MaximizeSpell(metaMagic, 8, caster.CasterLevel);
        damage = SpellUtils.EmpowerSpell(metaMagic, damage);
        if (savingThrowResult == SavingThrowResult.Success) damage /= 2;

        ApplyDamage(damage, targetObject);
    }

    private static void ApplyDamage(int damage, NwGameObject targetObject)
    {
        targetObject.ApplyEffect(EffectDuration.Instant, Effect.Damage(damage, DamageType.Divine));
        targetObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpSunstrike));
    }

    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
}
