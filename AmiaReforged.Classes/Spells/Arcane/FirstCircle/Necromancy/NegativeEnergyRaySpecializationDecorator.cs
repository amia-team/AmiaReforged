using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells.Arcane.FirstCircle.Necromancy;

[DecoratesSpell(typeof(NegativeEnergyRay))]
public class NegativeEnergyRaySpecializationDecorator : SpellDecorator
{
    private const double FiveRounds = 30;
    private const int SavingThrowPenalty = 2;
    private const string EffectTag = "NegativeEnergyRaySpecializationDecorator";

    public NegativeEnergyRaySpecializationDecorator(ISpell spell) : base(spell)
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

        bool isNecromancySpecialist = casterCreature.GetSpecialization(NwClass.FromClassType(ClassType.Wizard)) ==
                                       SpellSchool.Necromancy;

        if (isNecromancySpecialist && !ResistedSpell)
        {
            Effect savePenalty = Effect.SavingThrowDecrease(SavingThrow.All, SavingThrowPenalty);
            Effect vfx = Effect.VisualEffect(VfxType.DurAuraRedDark);
            savePenalty = Effect.LinkEffects(savePenalty, vfx);
            savePenalty.Tag = EffectTag;

            RemoveExistingEffect(creature);
            creature.ApplyEffect(EffectDuration.Temporary, savePenalty, TimeSpan.FromSeconds(FiveRounds));
        }

        Spell.OnSpellImpact(eventData);
    }

    private static void RemoveExistingEffect(NwCreature creature)
    {
        Effect? existing = creature.ActiveEffects.FirstOrDefault(e => e.Tag == EffectTag);
        if (existing != null) creature.RemoveEffect(existing);
    }
}

