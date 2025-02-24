using Anvil.API;
using Anvil.API.Events;
using NLog;
using NLog.Fluent;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips;

// [DecoratesSpell(typeof(ElectricJolt))]
public class ElectricJoltFocusDecorator : SpellDecorator
{
    private const double TwoRounds = 12;

    public ElectricJoltFocusDecorator(ISpell spell) : base(spell)
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

        LogManager.GetCurrentClassLogger().Info("Electric Jolt focus decorator");

        bool basicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusEvocation);
        bool greaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusEvocation);
        bool epicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusEvocation);

        bool isEvocationFocused = basicFocus || greaterFocus || epicFocus;

        if (isEvocationFocused && Result == ResistSpellResult.Failed)
        {
            int electricSavePenalty = epicFocus ? 3 : greaterFocus ? 2 : basicFocus ? 1 : 0;

            Effect savePenalty =
                Effect.SavingThrowDecrease(SavingThrow.All, electricSavePenalty, SavingThrowType.Electricity);
            savePenalty.Tag = "ElecJoltFocusDecorator";
            
            Effect? existing = creature.ActiveEffects.FirstOrDefault(e => e.Tag == "ElecJoltFocusDecorator");
            
            if(existing != null) creature.RemoveEffect(existing);
            
            target.ApplyEffect(EffectDuration.Temporary, savePenalty, TimeSpan.FromSeconds(TwoRounds));
        }

        Spell.OnSpellImpact(eventData);
    }
}