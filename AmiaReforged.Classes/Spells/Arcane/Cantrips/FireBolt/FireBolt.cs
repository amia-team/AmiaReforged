using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.FireBolt;

[ServiceBinding(typeof(ISpell))]
public class FireBolt : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }

    public string ImpactScript => "X0_S0_Flare";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        if (caster is not NwCreature casterCreature) return;

        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        ApplyBolt(target);

        int damage = CalculateDamage(caster);

        if (!ResistedSpell) return;

        ApplyDamage(damage, target);
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    private void ApplyBolt(NwGameObject target)
    {
        Effect fireBolt = Effect.VisualEffect(VfxType.ImpFlameS);
        target.ApplyEffect(EffectDuration.Instant, fireBolt);
    }

    private void ApplyDamage(int damage, NwGameObject target)
    {
        Effect damageEffect = Effect.Damage(damage, DamageType.Fire);
        target.ApplyEffect(EffectDuration.Instant, damageEffect);
    }

    private int CalculateDamage(NwGameObject caster)
    {
        int numDie = caster.CasterLevel / 2;
        int damage = NWScript.d3(numDie);

        return damage;
    }
}