using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.Resistance;

[ServiceBinding(typeof(ISpell))]
public class Resistance : ISpell
{
    public ResistSpellResult Result { get; set; }
    public string ImpactScript { get; }

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        Result = creature.CheckResistSpell(caster);
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;

        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        if (caster is not NwCreature casterCreature) return;
        
        bool spellFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusAbjuration);
        bool greaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusAbjuration);
        bool epicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusAbjuration);
        
        bool isAbjurationFocused = spellFocus || greaterFocus || epicFocus;

        int bonusTurns = spellFocus ? 1 : greaterFocus ? 2 : epicFocus ? 3 : 0;
        int turns = 2 + bonusTurns;
        
        bool isSpecialist = casterCreature.GetSpecialization(NwClass.FromClassType(ClassType.Wizard)) == SpellSchool.Abjuration;
        
        if (isAbjurationFocused)
        {
            Effect saveIncrease = Effect.SavingThrowIncrease(SavingThrow.All, 1, SavingThrowType.All);
            saveIncrease.Tag = "Resistance";
            
            Effect saveIncreaseSpecialist = Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Spell);
            
            if(isSpecialist) saveIncrease = Effect.LinkEffects(saveIncrease, saveIncreaseSpecialist);
            
            Effect? existing = target.ActiveEffects.FirstOrDefault(e => e.Tag == "Resistance");
            
            if(existing != null) target.RemoveEffect(existing);
            
            target.ApplyEffect(EffectDuration.Temporary, saveIncrease, TimeSpan.FromMinutes(turns));
        }

    }

    public void SetResult(ResistSpellResult result)
    {
        Result = result;
    }
}