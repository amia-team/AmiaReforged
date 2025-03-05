using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.BallOfSound;

[DecoratesSpell(typeof(BallOfSound))]
public class RaucousResonance : SpellDecorator
{
    private const double TwoRounds = 12;

    public RaucousResonance(ISpell spell) : base(spell)
    {
        Spell = spell;
    }

    public override void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster == null) return;
        if (eventData.TargetObject == null) return;

        bool targetNotDeaf = eventData.TargetObject.ActiveEffects.All(e => e.EffectType != EffectType.Deaf);

        if (eventData.Caster is NwCreature casterCreature)
        {
            bool isSpecialized = casterCreature.GetSpecialization(NwClass.FromClassType(ClassType.Wizard)) ==
                                 SpellSchool.Transmutation;

            if (isSpecialized && targetNotDeaf)
            {
                Effect sonicVulnerability = Effect.DamageImmunityDecrease(DamageType.Sonic, 10);
                sonicVulnerability.Tag = "RaucousResonance";

                Effect? existingEffect =
                    eventData.TargetObject.ActiveEffects.FirstOrDefault(e => e.Tag == "RaucousResonance");

                if (existingEffect != null)
                {
                    eventData.TargetObject.RemoveEffect(existingEffect);
                }


                eventData.TargetObject.ApplyEffect(EffectDuration.Temporary, sonicVulnerability,
                    TimeSpan.FromSeconds(TwoRounds));
            }
        }

        Spell.OnSpellImpact(eventData);
    }
}