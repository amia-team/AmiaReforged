using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips;

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

        bool hasNecroFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusNecromancy);
        bool hasGreaterNecroFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusNecromancy);
        bool hasEpicNecroFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusNecromancy);

        bool anyFocus = hasNecroFocus || hasGreaterNecroFocus || hasEpicNecroFocus;

        if (creature.Race.RacialType == RacialType.Undead && anyFocus)
        {
            int reductionAmount = hasNecroFocus ? 1 : hasGreaterNecroFocus ? 2 : hasEpicNecroFocus ? 3 : 0;
            int immunityReduction = hasEpicNecroFocus ? 5 : 0;

            Effect saveDecrease = Effect.SavingThrowDecrease(SavingThrow.Will, reductionAmount);
            saveDecrease = Effect.LinkEffects(Effect.DamageImmunityDecrease(DamageType.Positive, immunityReduction), saveDecrease);
            saveDecrease.Tag = "DisruptUndeadFocusDecorator";
            
            Effect? existing = creature.ActiveEffects.FirstOrDefault(e => e.Tag == "DisruptUndeadFocusDecorator");
            if(existing != null) creature.RemoveEffect(existing);
            
            if (Result == ResistSpellResult.Failed)
            {
                target.ApplyEffect(EffectDuration.Temporary, saveDecrease, TimeSpan.FromSeconds(12));
            }
        }

        Spell.OnSpellImpact(eventData);
    }
}