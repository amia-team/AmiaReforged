using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.FireBolt;

[DecoratesSpell(typeof(FireBolt))]
public class FireBoltFocusDecorator : SpellDecorator
{
    public FireBoltFocusDecorator(ISpell spell) : base(spell)
    {
        Spell = spell;
    }

    // Applies a reduction to saves vs fire, vulnerability based on spell focus feats.
    public override void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        if (caster is not NwCreature casterCreature) return;

        bool basicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusEvocation);
        bool greaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusEvocation);
        bool epicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusEvocation);

        bool isEvocationFocused = basicFocus || greaterFocus || epicFocus;

        if (isEvocationFocused && Result == ResistSpellResult.Failed)
        {
            int fireSavePenalty = epicFocus ? 3 : greaterFocus ? 2 : basicFocus ? 1 : 0;
            int extraVulnerability = epicFocus ? 5 : 0;
            Effect savePenalty = Effect.SavingThrowDecrease(SavingThrow.All, fireSavePenalty, SavingThrowType.Fire);
            savePenalty = Effect.LinkEffects(Effect.DamageImmunityDecrease(DamageType.Fire, extraVulnerability),
                savePenalty);
            savePenalty.Tag = "FireBoltFocusDecorator";

            Effect? existing = target.ActiveEffects.FirstOrDefault(e => e.Tag == "FireBoltFocusDecorator");

            if (existing != null) target.RemoveEffect(existing);

            target.ApplyEffect(EffectDuration.Temporary, savePenalty, TimeSpan.FromSeconds(12));
        }

        Spell.OnSpellImpact(eventData);
    }
}