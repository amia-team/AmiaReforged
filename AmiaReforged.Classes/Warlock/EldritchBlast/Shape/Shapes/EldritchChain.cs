using AmiaReforged.Classes.Spells;
using AmiaReforged.Classes.Warlock.EldritchBlast.Essence;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Warlock.EldritchBlast.BlastMechanics;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Shape.Shapes;

[ServiceBinding(typeof(IShape))]
public class EldritchChain : IShape
{
    public ShapeType ShapeType => ShapeType.Chain;

    public void CastEldritchShape(NwCreature warlock, int invocationCl, int invocationDc, EssenceData essence,
        SpellEvents.OnSpellCast eventData)
    {
        if (eventData.TargetObject is not { } targetObject || eventData.Spell is not { } spell
            || targetObject is NwCreature targetCreature && warlock.IsReactionTypeFriendly(targetCreature))
            return;

        SpellUtils.SignalSpell(caster: warlock, targetObject, spell);

        TouchAttackResult touchAttackResult = warlock.EldritchTouchAttack(targetObject);

        ApplyChainVfx(targetObject, warlock, essence.BeamVfx, BodyNode.Hand, touchAttackResult);

        if (touchAttackResult == TouchAttackResult.Miss ||
            !essence.BypassSpellResistance && warlock.InvocationResistCheck(targetObject, invocationCl, true))
            return;

        (int FlatBonus, double Multiplier) damageModifiers = GetEldritchDamageModifiers(warlock, invocationCl);

        int damage = RollEldritchDamage(damageModifiers, invocationCl, touchAttackResult);

        targetObject.ApplyEldritchBlast(warlock, damage, invocationDc, essence);

        if (targetObject.Location == null || targetObject is not NwCreature creature) return;

        BlastContext blast = new
        (
            Warlock: warlock,
            InvocationCl: invocationCl,
            InvocationDc: invocationDc,
            Essence: essence,
            Spell: spell,
            DamageModifiers: damageModifiers
        );

        int remainingChains = Math.Max(1, invocationCl / 5);

        HashSet<NwCreature> previouslyHitCreatures = [creature];

        _ = ChainEldritchBlast(creature, blast, remainingChains, previouslyHitCreatures);
    }

    private static async Task ChainEldritchBlast(NwCreature currentSource, BlastContext blast, int remainingChains,
        HashSet<NwCreature> previouslyHitCreatures)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(0.4));
        await blast.Warlock.WaitForObjectContext();

        if (currentSource.Location is not { } location) return;

        foreach (NwCreature creature in location.GetObjectsInShapeByType<NwCreature>(Anvil.API.Shape.Sphere, RadiusSize.Huge,
                     true))
        {
            if (!blast.Warlock.IsReactionTypeHostile(creature) || creature.IsDead || !creature.IsValid ||
                previouslyHitCreatures.Contains(creature))
                continue;

            CreatureEvents.OnSpellCastAt.Signal(blast.Warlock, creature, blast.Spell);
            ApplyChainVfx(creature, currentSource, blast.Essence.BeamVfx, bodyNode: BodyNode.Chest);

            // We want to stop the chain in its tracks if a creature successfully resists it
            if (!blast.Essence.BypassSpellResistance &&
                blast.Warlock.InvocationResistCheck(creature, blast.InvocationCl, true)) return;

            int damage = RollEldritchDamage(blast.DamageModifiers, blast.InvocationCl) / 2;

            creature.ApplyEldritchBlast(blast.Warlock, damage, blast.InvocationDc, blast.Essence);

            previouslyHitCreatures.Add(creature);

            remainingChains--;

            if (remainingChains <= 0) return;

            currentSource = creature;

            _ = ChainEldritchBlast(currentSource, blast, remainingChains, previouslyHitCreatures);
            return;
        }
    }

    private record BlastContext(NwCreature Warlock, int InvocationCl, int InvocationDc, EssenceData Essence,
        NwSpell Spell, (int FlatBonus, double Multiplier) DamageModifiers);

    private static void ApplyChainVfx(NwGameObject targetObject, NwGameObject currentSource,
        VfxType beamVfx, BodyNode bodyNode, TouchAttackResult? touchAttackResult = null)
    {
        bool missEffect = touchAttackResult == TouchAttackResult.Miss;

        Effect chainVfx = Effect.LinkEffects
        (
            Effect.Beam(beamVfx, currentSource, bodyNode, missEffect),
            Effect.Beam(VfxType.BeamChain, currentSource, bodyNode, missEffect)
        );
        TimeSpan beamDuration = TimeSpan.FromSeconds(2);

        targetObject.ApplyEffect(EffectDuration.Temporary, chainVfx, beamDuration);
    }
}
