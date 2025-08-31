using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.SixthCircle.Evocation;


[ServiceBinding(typeof(ISpell))]
public class ChainLightning : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_ChLightn";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;
        if (caster.Location == null) return;

        int arcs =
            caster.KnowsFeat(Feat.EpicSpellFocusEvocation!) ? 6 :
            caster.KnowsFeat(Feat.GreaterSpellFocusEvocation!) ? 5 :
            caster.KnowsFeat(Feat.SpellFocusEvocation!) ? 4 :
            3;

        int damageDice = caster.GetClassInfo(eventData.SpellCastClass)?.Level > 20 ? 20 :
            caster.GetClassInfo(eventData.SpellCastClass)?.Level ?? 0;

        int spellDc = SpellUtils.GetSpellDc(eventData);

        MetaMagic metaMagic = eventData.MetaMagicFeat;

        Location? spellLocation = eventData.TargetObject?.Location;
        if (spellLocation == null) return;

        foreach (NwCreature hostileCreature in spellLocation
                     .GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Colossal,true)
                     .Where(caster.IsReactionTypeHostile))
        {
            if (arcs == 0) break;
            if (hostileCreature.IsDead) continue;
            _ = ShootArc(caster, hostileCreature, damageDice, spellDc, metaMagic);
            arcs--;
        }
    }

    private async Task ShootArc(NwCreature caster, NwCreature hostileCreature, int damageDice, int spellDc,
        MetaMagic metaMagic)
    {
        float delay = caster.Distance(hostileCreature) / 10;
        await NwTask.Delay(TimeSpan.FromSeconds(delay));

        hostileCreature.ApplyEffect(EffectDuration.Temporary,
            Effect.Beam(VfxType.BeamLightning, caster, BodyNode.Hand),
            TimeSpan.FromSeconds(0.5));

        await caster.WaitForObjectContext();
        RollDamage(hostileCreature, caster, damageDice, spellDc, metaMagic);

        if (hostileCreature.Location == null) return;

        await NwTask.Delay(TimeSpan.FromSeconds(0.25f));

        foreach (NwCreature secondaryCreature in hostileCreature.Location
                     .GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large,true)
                     .Where(caster.IsReactionTypeHostile))
        {
            secondaryCreature.ApplyEffect(EffectDuration.Temporary,
                Effect.Beam(VfxType.BeamLightning, hostileCreature, BodyNode.Hand),
                TimeSpan.FromSeconds(0.5));

            damageDice /= 2;

            RollDamage(secondaryCreature, caster, damageDice, spellDc, metaMagic);
        }
    }


    private void RollDamage(NwCreature targetCreature, NwCreature caster, int damageDice, int spellDc,
        MetaMagic metaMagic)
    {
        int damage = SpellUtils.MaximizeSpell(metaMagic, 6, damageDice);
        SpellUtils.EmpowerSpell(metaMagic, damage);

        SavingThrowResult savingThrow =
            targetCreature.RollSavingThrow(SavingThrow.Reflex, spellDc, SavingThrowType.Electricity, caster);

        if (savingThrow == SavingThrowResult.Success || targetCreature.KnowsFeat(Feat.ImprovedEvasion!))
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
            damage /= 2;
        }

        if (targetCreature.KnowsFeat(Feat.ImprovedEvasion!) || targetCreature.KnowsFeat(Feat.Evasion!)
            && savingThrow == SavingThrowResult.Success)
        {
            return;
        }

        Effect damageEffect = Effect.LinkEffects(
            Effect.Damage(damage, DamageType.Electrical),
            Effect.VisualEffect(VfxType.ImpLightningS)
        );

        targetCreature.ApplyEffect(EffectDuration.Instant, damageEffect);
    }


    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}
