using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using AmiaReforged.Classes.Spells;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.GeneralFeats;

[ServiceBinding(typeof(ISpell))]
public class BlindingSpeed(CooldownService cooldownService) : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "x2_s2_blindspd";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature creature) return;

        string effectName = eventData.Spell.Name.ToString();

        if (cooldownService.IsOnCooldown(creature, effectName))
            return;

        Effect blindingSpeed = Effect.LinkEffects(Effect.Haste(), Effect.VisualEffect(VfxType.DurCessatePositive));
        blindingSpeed.SubType = EffectSubType.Extraordinary;

        creature.ApplyEffect(EffectDuration.Temporary, blindingSpeed, TimeSpan.FromSeconds(180));
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDustExplosion));
        cooldownService.ApplyCooldown(creature, effectName, TimeSpan.FromSeconds(210));
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}


