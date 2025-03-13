using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NLog.Fluent;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.RayofFrost;

[ServiceBinding(typeof(ISpell))]
public class RayOfFrost : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }

    public string ImpactScript => "NW_S0_RayFrost";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        if (caster is not NwCreature) return;

        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        Effect beam = Effect.Beam(VfxType.BeamCold, caster, BodyNode.Hand);
        target.ApplyEffect(EffectDuration.Temporary, beam, TimeSpan.FromSeconds(1.1));

        int numberOfDie = caster.CasterLevel / 2;
        LogManager.GetCurrentClassLogger().Info($"Number of die: {numberOfDie}");
        int damage = NWScript.d3(numberOfDie);

        Effect damageEffect = Effect.Damage(damage, DamageType.Cold);

        if (!ResistedSpell) target.ApplyEffect(EffectDuration.Instant, damageEffect);
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}