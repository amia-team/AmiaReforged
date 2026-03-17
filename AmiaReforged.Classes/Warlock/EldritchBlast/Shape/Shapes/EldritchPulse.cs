using AmiaReforged.Classes.Warlock.EldritchBlast.Essence;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Warlock.EldritchBlast.BlastMechanics;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Shape.Shapes;

[ServiceBinding(typeof(IShape))]
public class EldritchPulse(ScriptHandleFactory scriptHandleFactory) : IShape
{
    public ShapeType ShapeType => ShapeType.Pulse;

    public void CastEldritchShape(NwCreature warlock, int invocationCl, int invocationDc, EssenceData essence,
        SpellEvents.OnSpellCast eventData)
    {
        if (eventData.TargetObject is not { } targetObject || eventData.Spell is not { } spell) return;

        (int FlatBonus, double Multiplier) damageModifiers = GetEldritchDamageModifiers(warlock, invocationCl);

        BlastContext blast = new
        (
            Warlock: warlock,
            Cl: invocationCl,
            Dc: invocationDc,
            Essence: essence,
            Spell: spell,
            DamageModifiers: damageModifiers
        );

        ScriptCallbackHandle pulseHandle = scriptHandleFactory.CreateUniqueHandler(_ => Pulse(targetObject, blast));

        Effect eldritchPulse = Effect.RunAction(onAppliedHandle: pulseHandle, onRemovedHandle: pulseHandle);
        targetObject.ApplyEffect(EffectDuration.Temporary, eldritchPulse, NwTimeSpan.FromRounds(1));
    }

    private static ScriptHandleResult Pulse(NwGameObject targetObject, BlastContext blast)
    {
        if (targetObject.Location is not { } location) return ScriptHandleResult.Handled;

        Effect fortVfx = Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse);

        targetObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(blast.Essence.PulseVfx));

        foreach (NwCreature creature in location.GetObjectsInShapeByType<NwCreature>(Anvil.API.Shape.Sphere, RadiusSize.Large,
                     losCheck: true))
        {
            if (!creature.IsValidInvocationTarget(blast.Warlock, hurtSelf: false) || creature.IsDead) continue;

            CreatureEvents.OnSpellCastAt.Signal(blast.Warlock, creature, blast.Spell);

            if (!blast.Essence.BypassSpellResistance && blast.Warlock.InvocationResistCheck(creature, blast.Cl, true))
                continue;

            SavingThrowResult fortSave
                = creature.RollSavingThrow(SavingThrow.Fortitude, blast.Dc, SavingThrowType.Spell, blast.Warlock);

            int damage = RollEldritchDamage(blast.DamageModifiers, blast.Cl) / 2;

            if (fortSave == SavingThrowResult.Success)
            {
                creature.ApplyEffect(EffectDuration.Instant, fortVfx);
                damage /= 2;
            }

            creature.ApplyEldritchBlast(blast.Warlock, damage, blast.Dc, blast.Essence);
        }

        return ScriptHandleResult.Handled;
    }

    private record BlastContext(NwCreature Warlock, int Cl, int Dc, EssenceData Essence, NwSpell Spell,
        (int FlatBonus, double Multiplier) DamageModifiers);
}
