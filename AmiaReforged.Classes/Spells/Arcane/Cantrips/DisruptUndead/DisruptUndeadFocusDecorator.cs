using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.DisruptUndead;

[DecoratesSpell(typeof(DisruptUndead))]
public class DisruptUndeadFocusDecorator : SpellDecorator
{
    public DisruptUndeadFocusDecorator(ISpell spell) : base(spell)
    {
        Spell = spell;
    }

    public override void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        if (target is not NwCreature creature) return;
        if (caster is not NwCreature casterCreature) return;

        bool hasNecroFocus = HasAnyFocus(casterCreature, out bool hasGreaterNecroFocus, out bool hasEpicNecroFocus, out bool anyFocus);

        Effect saveDecrease = CreatePenaltyEffect(hasNecroFocus, hasGreaterNecroFocus, hasEpicNecroFocus);


        if (creature.Race.RacialType == RacialType.Undead && anyFocus)
        {
            ApplyPenalty(creature, target, saveDecrease);
        }

        Spell.OnSpellImpact(eventData);
    }

    private static bool HasAnyFocus(NwCreature casterCreature, out bool hasGreaterNecroFocus, out bool hasEpicNecroFocus,
        out bool anyFocus)
    {
        bool hasNecroFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusNecromancy);
        hasGreaterNecroFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusNecromancy);
        hasEpicNecroFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusNecromancy);
        anyFocus = hasNecroFocus || hasGreaterNecroFocus || hasEpicNecroFocus;
        return hasNecroFocus;
    }

    private static Effect CreatePenaltyEffect(bool hasNecroFocus, bool hasGreaterNecroFocus, bool hasEpicNecroFocus)
    {
        int reductionAmount = hasNecroFocus ? 1 : hasGreaterNecroFocus ? 2 : hasEpicNecroFocus ? 3 : 0;
        int immunityReduction = hasEpicNecroFocus ? 5 : 0;
        Effect saveDecrease = Effect.SavingThrowDecrease(SavingThrow.Will, reductionAmount);
        saveDecrease = Effect.LinkEffects(Effect.DamageImmunityDecrease(DamageType.Positive, immunityReduction), saveDecrease);
        saveDecrease.Tag = "DisruptUndeadFocusDecorator";
        return saveDecrease;
    }

    private void ApplyPenalty(NwCreature creature, NwGameObject target, Effect saveDecrease)
    {
        if (Result == ResistSpellResult.Failed)
        {
            RemoveExistingEffect(creature);
            target.ApplyEffect(EffectDuration.Temporary, saveDecrease, TimeSpan.FromSeconds(12));
        }
    }

    private static void RemoveExistingEffect(NwCreature creature)
    {
        Effect? existing = creature.ActiveEffects.FirstOrDefault(e => e.Tag == "DisruptUndeadFocusDecorator");
        if(existing != null) creature.RemoveEffect(existing);
    }
}