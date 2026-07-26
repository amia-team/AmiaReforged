using AmiaReforged.Classes.EffectUtils.MagicMissile;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.FourthCircle.Evocation;

[ServiceBinding(typeof(ISpell))]
public class IsaacsLesserMissileStorm(MagicMissileService magicMissileService) : ISpell
{
    public string ImpactScript => "x0_s0_missstorm1";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster || eventData.TargetLocation is not { } location)
            return;

        magicMissileService.ApplyMissiles(caster, location, eventData.Spell, eventData.MetaMagicFeat);
    }

    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
}
