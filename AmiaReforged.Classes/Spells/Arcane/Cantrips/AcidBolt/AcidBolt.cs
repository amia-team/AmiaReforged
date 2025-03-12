using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.AcidBolt;

/// <summary>
///     Acid splash has been replaced with Acid Bolt.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class AcidBolt : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "X0_S0_AcidSplash";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        if (caster is not NwCreature casterCreature) return;

        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        ApplyBolt(target);

        int damage = CalculateDamage(casterCreature, caster);

        if (!ResistedSpell) return;

        ApplyDamage(damage, target);
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    private static void ApplyBolt(NwGameObject target)
    {
        Effect bolt = Effect.VisualEffect(VfxType.ImpAcidS);
        target.ApplyEffect(EffectDuration.Instant, bolt);
    }

    private static int CalculateDamage(NwCreature casterCreature, NwGameObject caster)
    {
        int damageBonus = EvaluateBonusDamage(casterCreature);

        int numberOfDie = caster.CasterLevel / 2;
        int damage = NWScript.d3(numberOfDie) + damageBonus;
        return damage;
    }

    private static int EvaluateBonusDamage(NwCreature casterCreature)
    {
        bool hasFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusEvocation);
        bool hasGreaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusEvocation);
        bool hasEpicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusEvocation);

        int damageBonus = hasFocus ? 2 : hasGreaterFocus ? 4 : hasEpicFocus ? 6 : 0;

        return damageBonus;
    }

    private static void ApplyDamage(int damage, NwGameObject target)
    {
        Effect damageEffect = Effect.Damage(damage, DamageType.Acid);
        target.ApplyEffect(EffectDuration.Instant, damageEffect);
    }
}