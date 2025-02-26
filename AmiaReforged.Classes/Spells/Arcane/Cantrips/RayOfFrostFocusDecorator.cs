using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips;

[DecoratesSpell(typeof(RayOfFrost))]
public class RayOfFrostFocusDecorator : SpellDecorator
{
    private const double TwoRounds = 12;

    public RayOfFrostFocusDecorator(ISpell spell) : base(spell)
    {
        Spell = spell;
    }

    public override string ImpactScript => Spell.ImpactScript;

    public override void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;
        if (target is not NwCreature creature) return;

        if (caster is not NwCreature casterCreature) return;

        bool hasFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusEvocation);
        bool hasGreaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusEvocation);
        bool hasEpicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusEvocation);

        bool anyFocus = hasFocus || hasGreaterFocus || hasEpicFocus;

        if (anyFocus && Result == ResistSpellResult.Failed)
        {
            int savePenalty = hasEpicFocus ? 3 : hasGreaterFocus ? 2 : hasFocus ? 1 : 0;
            int freezeChance = hasEpicFocus ? 10 : 0;
            int rollPercentile = NWScript.d100();

            if (rollPercentile <= freezeChance && !creature.IsImmuneTo(ImmunityType.Slow))
            {
                Effect freeze = Effect.Slow();
                freeze = Effect.LinkEffects(freeze, Effect.VisualEffect(VfxType.DurIceskin));
                freeze.Tag = "RayOfFrostFocusDecorator";
                target.ApplyEffect(EffectDuration.Temporary, freeze, TimeSpan.FromSeconds(TwoRounds));
            }

            Effect savePenaltyEffect = Effect.SavingThrowDecrease(SavingThrow.All, savePenalty, SavingThrowType.Cold);

            target.ApplyEffect(EffectDuration.Temporary, savePenaltyEffect, TimeSpan.FromSeconds(TwoRounds));
        }

        Spell.OnSpellImpact(eventData);
    }
}