using Anvil.API;

namespace AmiaReforged.Classes.Spells;

/// <summary>
/// A public breach list you can use; based on nw_i0_spells.nss
/// </summary>
public class BreachList
{
    private const Spell GreaterSanctuary = (Spell)443;

    public List<Spell> BreachSpells = new()
    {
        Spell.GreaterSpellMantle,
        Spell.Premonition,
        Spell.ShadowShield,
        Spell.GreaterStoneskin,
        Spell.GlobeOfInvulnerability,
        Spell.EnergyBuffer,
        GreaterSanctuary,
        Spell.MinorGlobeOfInvulnerability,
        Spell.SpellResistance,
        Spell.Stoneskin,
        Spell.LesserSpellMantle,
        Spell.MestilsAcidSheath,
        Spell.MindBlank,
        Spell.ElementalShield,
        Spell.ProtectionFromSpells,
        Spell.ResistElements,
        Spell.DeathArmor,
        Spell.GhostlyVisage,
        Spell.ShadowShield,
        Spell.ShadowConjurationMageArmor,
        Spell.NegativeEnergyProtection,
        Spell.Sanctuary,
        Spell.MageArmor,
        Spell.StoneBones,
        Spell.Shield,
        Spell.ShieldOfFaith,
        Spell.LesserMindBlank,
        Spell.Ironguts,
        Spell.Resistance
    };
}