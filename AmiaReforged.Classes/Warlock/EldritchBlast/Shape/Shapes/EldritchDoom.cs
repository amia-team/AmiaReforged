using AmiaReforged.Classes.Spells;
using AmiaReforged.Classes.Warlock.EldritchBlast.Essence;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Warlock.EldritchBlast.BlastMechanics;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Shape.Shapes;

[ServiceBinding(typeof(IShape))]
public class EldritchDoom : IShape
{
    public ShapeType ShapeType => ShapeType.Doom;

    public void CastEldritchShape(NwCreature warlock, int warlockLevel, int invocationDc, EssenceData essence,
        SpellEvents.OnSpellCast eventData)
    {
        if (warlock.Area == null || eventData.Spell is not { } spell || eventData.TargetLocation is not { } location)
            return;

        (int FlatBonus, double Multiplier) damageModifiers = GetEldritchDamageModifiers(warlock, warlockLevel);

        Effect reflexVfx = Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse);

        BlastContext blast = new
        (
            Warlock: warlock,
            WarlockLevel: warlockLevel,
            InvocationDc: invocationDc,
            Essence: essence,
            Spell: spell,
            DamageModifiers: damageModifiers
        );

        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(essence.DoomVfx));

        foreach (NwCreature creature in location.GetObjectsInShapeByType<NwCreature>(Anvil.API.Shape.Sphere,
                     RadiusSize.Large, losCheck: true))
        {
            _ = ApplyDoom(creature, blast, reflexVfx);
        }
    }

    private static async Task ApplyDoom(NwCreature creature, BlastContext blast, Effect reflexVfx)
    {
        await NwTask.Delay(SpellUtils.GetRandomDelay(0.3, 0.6));
        await blast.Warlock.WaitForObjectContext();

        if (!creature.IsValidInvocationTarget(blast.Warlock) || creature.IsDead)
            return;

        CreatureEvents.OnSpellCastAt.Signal(blast.Warlock, creature, blast.Spell);

        if (!blast.Essence.BypassSpellResistance
            && blast.Warlock.InvocationResistCheck(creature, blast.WarlockLevel, true))
            return;

        SavingThrowResult reflexSave = creature.RollSavingThrow(SavingThrow.Reflex, blast.InvocationDc,
            SavingThrowType.Spell, blast.Warlock);

        bool hasImpEvasion = creature.KnowsFeat(Feat.ImprovedEvasion!);

        if (reflexSave == SavingThrowResult.Success)
        {
            creature.ApplyEffect(EffectDuration.Instant, reflexVfx);

            if (creature.KnowsFeat(Feat.Evasion!) || hasImpEvasion) return;
        }

        int damage = RollEldritchDamage(blast.DamageModifiers, blast.WarlockLevel);

        if (reflexSave == SavingThrowResult.Success || hasImpEvasion)
            damage /= 2;

        creature.ApplyEldritchBlast(blast.Warlock, damage, blast.InvocationDc, blast.Essence);
    }

    private record BlastContext(NwCreature Warlock, int WarlockLevel, int InvocationDc, EssenceData Essence,
        NwSpell Spell, (int FlatBonus, double Multiplier) DamageModifiers);
}
