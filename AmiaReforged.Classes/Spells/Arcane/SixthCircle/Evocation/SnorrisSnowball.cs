using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Org.BouncyCastle.Asn1.Cmp;

namespace AmiaReforged.Classes.Spells.Arcane.SixthCircle.Evocation;

/// <summary>
/// Snorris' Snowball.
/// </summary>
/// <remarks>
/// Level: Wizard/Sorcerer 6, Bard 6
/// Components: V, S
/// Range: Long
/// Area of effect: Single
/// Duration: Instantaneous
/// Valid Metamagic: Empower, Maximize
/// Save: Reflex 1/2
/// Spell Resistance: Yes
///
/// The caster hurls a snowball at a target, making a ranged touch attack.
/// On a hit, the target takes 1d6 cold damage per caster level.
///
/// The snowball then bursts: creatures in a large take 1d6 cold damage per two caster levels.
/// Each Spell Focus (Evocation) increases the burst damage by 1d6,
/// and Epic Spell Focus (Evocation) increases the burst damage by 3d6.
/// A successful Reflex save halves burst damage.
/// If the touch attack hits the initial target, it does not take burst damage.
/// </remarks>
[ServiceBinding(typeof(ISpell))]
public class SnorrisSnowball : ISpell
{
    private const VfxType FnfFreezingSphere = (VfxType)2533;
    public string ImpactScript => "snorris_snowball";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.TargetObject is not { } target || eventData.Caster is not NwCreature caster
            || target.Location == null) return;

        bool spellResisted = caster.SpellResistanceCheck(target, eventData.Spell, caster.CasterLevel);

        Effect damageVfx = Effect.VisualEffect(VfxType.ImpFrostS);

        SpellUtils.SignalSpell(caster, target, eventData.Spell);
        TouchAttackResult touchAttackResult = TouchAttackResult.Miss;
        if (!spellResisted)
        {
            touchAttackResult = caster.TouchAttackRanged(target, true);
            if (touchAttackResult != TouchAttackResult.Miss)
            {
                ApplyDamage(target, damageVfx, caster.CasterLevel);
            }
        }

        int burstDamageDice = caster.CasterLevel / 2;
        int bonusDamageDice =
            caster.KnowsFeat(Feat.EpicSpellFocusEvocation!) ? 5 :
            caster.KnowsFeat(Feat.GreaterSpellFocusEvocation!) ? 2 :
            caster.KnowsFeat(Feat.SpellFocusEvocation!) ? 1 : 0;

        burstDamageDice += bonusDamageDice;
        Effect reflexVfx = Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse);

        target.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(FnfFreezingSphere));
        foreach (NwGameObject nwGameObject in target.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, true,
                     ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door))
        {
            if (nwGameObject == target && touchAttackResult != TouchAttackResult.Miss || spellResisted) continue;

            if (nwGameObject is NwDoor or NwPlaceable)
            {
                SpellUtils.SignalSpell(caster, nwGameObject, eventData.Spell);
                ApplyDamage(nwGameObject, damageVfx, burstDamageDice);
                continue;
            }

            NwCreature creature = (NwCreature)nwGameObject;

            if (!SpellUtils.IsValidHostileTarget(creature, caster)) continue;
            CreatureEvents.OnSpellCastAt.Signal(caster, creature, eventData.Spell);

            if (creature != target && caster.SpellResistanceCheck(creature, eventData.Spell, caster.CasterLevel))
                continue;

            SavingThrowResult savingThrowResult = creature.RollSavingThrow(SavingThrow.Reflex, eventData.SaveDC,
                SavingThrowType.Cold, caster);

            bool damageHalved = savingThrowResult == SavingThrowResult.Success || creature.KnowsFeat(Feat.ImprovedEvasion!);

            if (savingThrowResult == SavingThrowResult.Success) creature.ApplyEffect(EffectDuration.Instant, reflexVfx);

            if (creature.KnowsFeat(Feat.Evasion!) || creature.KnowsFeat(Feat.ImprovedEvasion!)
                && savingThrowResult == SavingThrowResult.Success) continue;

            ApplyDamage(creature, damageVfx, burstDamageDice, damageHalved);
        }
    }

    private void ApplyDamage(NwGameObject target, Effect damageVfx, int damageDice, bool damageHalved = false)
    {
        int damageRoll = Random.Shared.Roll(6, damageDice);
        if (damageHalved) damageRoll /= 2;

        Effect damageEffect = Effect.Damage(damageRoll, DamageType.Cold);
        target.ApplyEffect(EffectDuration.Instant, damageEffect);
        target.ApplyEffect(EffectDuration.Instant, damageVfx);
    }

    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
}
