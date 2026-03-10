using AmiaReforged.Classes.Spells;
using AmiaReforged.Classes.Warlock.EldritchBlast.Essence;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Warlock.EldritchBlast.BlastMechanics;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Shape.Shapes;

[ServiceBinding(typeof(IShape))]
public class EldritchBlast : IShape
{
    public ShapeType ShapeType => ShapeType.Blast;

    public void CastEldritchShape(NwCreature warlock, int warlockLevel, int invocationDc, EssenceData essence,
        SpellEvents.OnSpellCast eventData)
    {
        if (eventData.TargetObject is not { } targetObject || eventData.Spell is not { } spell) return;

        SpellUtils.SignalSpell(caster: warlock, targetObject, spell);

        TouchAttackResult touchAttackResult = warlock.EldritchTouchAttack(targetObject);

        ApplyBeamVfx(targetObject, warlock, essence.BeamVfx, touchAttackResult);

        if (touchAttackResult == TouchAttackResult.Miss
            || !essence.BypassSpellResistance && warlock.InvocationResistCheck(targetObject, warlockLevel, true))
            return;

        (int FlatBonus, double Multiplier) damageModifiers = GetEldritchDamageModifiers(warlock, warlockLevel);

        int damage = RollEldritchDamage(damageModifiers, warlockLevel, touchAttackResult);

        targetObject.ApplyEldritchBlast(warlock, damage, invocationDc, essence);
    }

    private static void ApplyBeamVfx(NwGameObject targetObject, NwGameObject warlock,
        VfxType beamVfx, TouchAttackResult? touchAttackResult = null)
    {
        bool missEffect = touchAttackResult == TouchAttackResult.Miss;

        TimeSpan beamDuration = TimeSpan.FromSeconds(1.1);

        targetObject.ApplyEffect(EffectDuration.Temporary,
            Effect.Beam(beamVfx, warlock, BodyNode.Hand, missEffect), beamDuration);
    }
}
