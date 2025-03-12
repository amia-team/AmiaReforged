using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.Resistance;

[ServiceBinding(typeof(ISpell))]
public class Resistance : ISpell
{
    public bool ResistedSpell { get; set; }

    public string ImpactScript => "NW_S0_Resis";

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        ResistedSpell = creature.SpellResistanceCheck(caster);
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;

        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        bool spellFocus = false;
        bool greaterFocus = false;
        bool epicFocus = false;
        bool isSpecialist = false;

        if (caster is NwCreature casterCreature)
        {
            spellFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusAbjuration);
            greaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusAbjuration);
            epicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusAbjuration);
            isSpecialist = casterCreature.GetSpecialization(NwClass.FromClassType(ClassType.Wizard)) ==
                           SpellSchool.Abjuration;
        }

        int bonusTurns = spellFocus ? 1 : greaterFocus ? 2 : epicFocus ? 3 : 0;
        int turns = 2 + bonusTurns;

        Effect saveIncrease = Effect.SavingThrowIncrease(SavingThrow.All, 1);
        saveIncrease.Tag = "Resistance";

        Effect saveIncreaseSpecialist = Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Spell);

        if (isSpecialist) saveIncrease = Effect.LinkEffects(saveIncrease, saveIncreaseSpecialist);

        Effect? existing = target.ActiveEffects.FirstOrDefault(e => e.Tag == "Resistance");
        if (existing != null) target.RemoveEffect(existing);
        target.ApplyEffect(EffectDuration.Temporary, saveIncrease, TimeSpan.FromMinutes(turns));
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}