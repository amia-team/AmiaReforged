using AmiaReforged.Classes.Spells;
using AmiaReforged.Classes.Warlock.EldritchBlast.Essence;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Warlock.EldritchBlast.BlastMechanics;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Shape.Shapes;

[ServiceBinding(typeof(IShape))]
public class EldritchSpear : IShape
{
    private const float LongDistance = 40f;
    public ShapeType ShapeType => ShapeType.Spear;

    public void CastEldritchShape(NwCreature warlock, int warlockLevel, int invocationDc, EssenceData essence,
        SpellEvents.OnSpellCast eventData)
    {
        if (eventData.TargetObject is not { } targetObject || eventData.Spell is not { } spell) return;

        SpellUtils.SignalSpell(caster: warlock, targetObject, spell);

        TouchAttackResult touchAttackResult = warlock.EldritchTouchAttack(targetObject);

        ApplySpearVfx(targetObject, warlock, essence.BeamVfx, touchAttackResult);

        // If the touch attack misses, nothing in line of spell gets hit
        if (touchAttackResult == TouchAttackResult.Miss) return;

        (int FlatBonus, double Multiplier) damageModifiers = GetEldritchDamageModifiers(warlock, warlockLevel);

        // Even if the target resists the spell, we continue to hurt targets in line of spell
        if (essence.BypassSpellResistance || !warlock.InvocationResistCheck(targetObject, warlockLevel, true))
        {
            int damage = RollEldritchDamage(damageModifiers, warlockLevel, touchAttackResult);
            targetObject.ApplyEldritchBlast(warlock, damage, invocationDc, essence);
        }

        if (targetObject.Location == null) return;

        Effect reflexVfx = Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse);

        foreach (NwCreature creature in targetObject.Location.GetObjectsInShapeByType<NwCreature>
                     (Anvil.API.Shape.SpellCylinder, size: LongDistance, true, warlock.Position))
        {
            if (creature.IsDead || creature == targetObject || !creature.IsValidInvocationTarget(warlock, hurtSelf: false))
                continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, spell);

            if (!essence.BypassSpellResistance && warlock.InvocationResistCheck(creature, warlockLevel, true))
                continue;

            SavingThrowResult reflexSave
                = creature.RollSavingThrow(SavingThrow.Reflex, invocationDc, SavingThrowType.Spell, warlock);

            bool hasImpEvasion = creature.KnowsFeat(Feat.ImprovedEvasion!);

            if (reflexSave == SavingThrowResult.Success)
            {
                creature.ApplyEffect(EffectDuration.Instant, reflexVfx);

                if (creature.KnowsFeat(Feat.Evasion!) || hasImpEvasion) continue;
            }

            int damage = RollEldritchDamage(damageModifiers, warlockLevel) / 2;

            if (reflexSave == SavingThrowResult.Success || hasImpEvasion)
                damage /= 2;

            creature.ApplyEldritchBlast(warlock, damage, invocationDc, essence);
        }
    }

    private static void ApplySpearVfx(NwGameObject targetObject, NwGameObject warlock, VfxType beamVfx,
        TouchAttackResult? touchAttackResult = null)
    {
        bool missEffect = touchAttackResult == TouchAttackResult.Miss;

        TimeSpan beamDuration = TimeSpan.FromSeconds(1.1);

        Effect spearVfx = Effect.Beam(beamVfx, warlock, BodyNode.Hand, missEffect);
        spearVfx.FloatParams[0] = 8.0f;

        targetObject.ApplyEffect(EffectDuration.Temporary, spearVfx, beamDuration);
    }
}
