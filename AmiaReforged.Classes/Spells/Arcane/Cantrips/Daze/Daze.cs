using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.Daze;

[ServiceBinding(typeof(ISpell))]
public class Daze : ISpell
{
    private const double TwoRounds = 12;
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_Daze";

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        ResistedSpell = creature.SpellResistanceCheck(caster);
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster == null) return;
        if (eventData.Caster is not NwCreature casterCreature) return;
        if (eventData.TargetObject == null) return;

        if (!ResistedSpell) return;

        Ability primaryAbility = Ability.Charisma;
        if (eventData.SpellCastClass != null) primaryAbility = eventData.SpellCastClass.PrimaryAbility;

        SavingThrowResult result = SavingThrowResult.Failure;


        if (eventData.TargetObject is NwCreature targetCreature)
        {
            int dc = 10 + eventData.SpellLevel + casterCreature.GetAbilityModifier(primaryAbility);

            result = targetCreature.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells);
        }

        if (result != SavingThrowResult.Failure) return;

        ApplyEffect(eventData);
    }

    private void ApplyEffect(SpellEvents.OnSpellCast eventData)
    {
        Effect? existingDaze =
            eventData.TargetObject!.ActiveEffects.SingleOrDefault(e => e.EffectType == EffectType.Dazed);

        if (existingDaze != null) return;

        Effect dazeEffect = Effect.Dazed();

        eventData.TargetObject.ApplyEffect(EffectDuration.Temporary, dazeEffect, TimeSpan.FromSeconds(TwoRounds));
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}