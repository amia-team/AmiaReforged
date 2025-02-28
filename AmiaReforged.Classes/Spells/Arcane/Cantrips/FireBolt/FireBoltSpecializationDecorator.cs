using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.FireBolt;

[DecoratesSpell(typeof(FireBolt))]
public class FireBoltSpecializationDecorator : SpellDecorator
{
    private const int TwoRounds = 12;
    
    public FireBoltSpecializationDecorator(ISpell spell) : base(spell)
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

        bool hasEvocationSpecialization = casterCreature.GetSpecialization(NwClass.FromClassType(ClassType.Wizard)) == SpellSchool.Evocation;

        if (hasEvocationSpecialization)
        {
            Effect attackBonus = Effect.AttackIncrease(1);
            attackBonus.Tag = "FireBoltSpecializationDecorator";
            
            ApplyAttackBonus(creature, attackBonus);
        }

        Spell.OnSpellImpact(eventData);
    }

    private void ApplyAttackBonus(NwCreature creature, Effect attackBonus)
    {
        Effect? existing = creature.ActiveEffects.FirstOrDefault(e => e.Tag == "FireBoltSpecializationDecorator");
        
        if(existing != null) creature.RemoveEffect(existing);
        
        creature.ApplyEffect(EffectDuration.Temporary, attackBonus, TimeSpan.FromSeconds(TwoRounds));
    }
}