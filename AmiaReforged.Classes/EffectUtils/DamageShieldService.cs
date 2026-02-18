using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.EffectUtils;

/// <summary>
/// This service holds functionality and rules for the stacking of damage shields, and it should be used for
/// spells that apply damage shield effects
/// </summary>
[ServiceBinding(typeof(DamageShieldService))]
public class DamageShieldService
{
    private const int MorgensElectrifierId = 859;
    private readonly NwSpell?[] _damageShieldSpells =
    [
        NwSpell.FromSpellType(Spell.ElementalShield),
        NwSpell.FromSpellType(Spell.MestilsAcidSheath),
        NwSpell.FromSpellType(Spell.WoundingWhispers),
        NwSpell.FromSpellType(Spell.DeathArmor),
        NwSpell.FromSpellId(MorgensElectrifierId)
    ];

    /// <summary>
    /// Amia's rules disallow stacking damage shield spells; this should be called before applying damage shield spells
    /// to the target creature
    /// </summary>
    public void RemoveDamageShieldSpells(NwCreature creature)
    {
        foreach (Effect effect in creature.ActiveEffects)
        {
            if (_damageShieldSpells.Contains(effect.Spell))
                creature.RemoveEffect(effect);
        }
    }
}
