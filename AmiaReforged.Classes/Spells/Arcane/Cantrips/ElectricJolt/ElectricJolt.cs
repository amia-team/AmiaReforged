using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.ElectricJolt;

[ServiceBinding(typeof(ISpell))]
public class ElectricJolt : ISpell
{
    public ResistSpellResult Result { get; set; }
    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        Result = creature.CheckResistSpell(caster);
    }

    public string ImpactScript => "X0_S0_ElecJolt";
    public void SetResult(ResistSpellResult result)
    {
        Result = result;
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if(caster == null) return;
        if(caster is not NwCreature casterCreature) return;
        
        NwGameObject? target = eventData.TargetObject;
        if(target == null) return;
        
        Effect beam = Effect.Beam(VfxType.BeamLightning, casterCreature, BodyNode.Hand);
        target.ApplyEffect(EffectDuration.Temporary, beam, TimeSpan.FromSeconds(1.1));
        
        int numberOfDie = caster.CasterLevel / 2;
        int damage = NWScript.d3(numberOfDie);
        
        if (Result != ResistSpellResult.Failed) return;
        
        Effect damageEffect = Effect.Damage(damage, DamageType.Electrical);
        target.ApplyEffect(EffectDuration.Instant, damageEffect);

        // The jolt will jump to the nearest enemy within 5m of the target.
        JumpToEnemyIfSpecialized(casterCreature, target, damage);
    }

    private void JumpToEnemyIfSpecialized(NwCreature casterCreature, NwGameObject creature, int damage)
    {
        if (!IsSpecialist(casterCreature)) return;
        
        Effect jumpBeam = Effect.Beam(VfxType.BeamLightning, creature, BodyNode.Chest);
        NwCreature? nearestEnemy = creature.GetNearestCreatures().FirstOrDefault(c => c.IsReactionTypeHostile(casterCreature) && c.Distance(creature) <= 5);
        
        if (nearestEnemy == null) return;
        
        DoSpellResist(nearestEnemy, casterCreature);
        if (Result != ResistSpellResult.Failed) return;
        
        int damageHalved = damage / 2;
        Effect jumpDamage = Effect.Damage(damageHalved, DamageType.Electrical);
                    
        nearestEnemy.ApplyEffect(EffectDuration.Instant, jumpBeam);
        nearestEnemy.ApplyEffect(EffectDuration.Instant, jumpDamage);
    }

    private static bool IsSpecialist(NwCreature casterCreature)
    {
        bool isSpecialist = casterCreature.GetSpecialization(NwClass.FromClassType(ClassType.Wizard)) == SpellSchool.Evocation;
        return isSpecialist;
    }
}