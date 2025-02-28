using Anvil.API;
using Anvil.API.Events;
using NLog;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.ElectricJolt;

[DecoratesSpell(typeof(ElectricJolt))]
public class ElectricJoltFocusDecorator : SpellDecorator
{
    private const double TwoRounds = 12;
    private const int EpicEvocationVulnerabilityPercentage = 5;
    private const int SpecialistVulnerabilityPercentage = 3;

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
        bool isSpecialist = casterCreature.GetSpecialization(NwClass.FromClassType(ClassType.Wizard)) == SpellSchool.Evocation;

        if (isEvocationFocused && Result == ResistSpellResult.Failed)
        {
            int electricSavePenalty = epicFocus ? 3 : greaterFocus ? 2 : basicFocus ? 1 : 0;
            int extraVulnerability = epicFocus && isSpecialist ? SpecialistVulnerabilityPercentage : 0;
            int immunityReduction = epicFocus ? EpicEvocationVulnerabilityPercentage + extraVulnerability : 0;
            Effect savePenalty =
                Effect.SavingThrowDecrease(SavingThrow.All, electricSavePenalty, SavingThrowType.Electricity);
            savePenalty = Effect.LinkEffects(Effect.DamageImmunityDecrease(DamageType.Electrical, immunityReduction), savePenalty);
            savePenalty.Tag = "ElecJoltFocusDecorator";
            
            Effect? existing = creature.ActiveEffects.FirstOrDefault(e => e.Tag == "ElecJoltFocusDecorator");
            
            if(existing != null) creature.RemoveEffect(existing);
            
            target.ApplyEffect(EffectDuration.Temporary, savePenalty, TimeSpan.FromSeconds(TwoRounds));
        }

        Spell.OnSpellImpact(eventData);
    }
}