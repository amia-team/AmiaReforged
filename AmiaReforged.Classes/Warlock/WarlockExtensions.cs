using AmiaReforged.Classes.Spells;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.Classes.Warlock;

public static class WarlockExtensions
{
    public const int WarlockId = 57;

    public static int WarlockLevel(this NwCreature warlock) => NWScript.GetLevelByClass(WarlockId, warlock);

    public static int InvocationDc(this NwCreature warlock, int warlockLevel) =>
        10 + warlock.GetAbilityModifier(Ability.Charisma) + warlockLevel / 3;

    /// <summary>
    /// Harmful invocations never hurt pact summons, but otherwise respect normal targeting rules.
    /// </summary>
    /// <returns>True if the spell should hit the target, false otherwise</returns>
    public static bool IsValidInvocationTarget(this NwCreature targetCreature, NwCreature warlock, bool hurtSelf = true)
    {
        if (targetCreature.IsPactSummon() || !hurtSelf && targetCreature == warlock) return false;

        return SpellUtils.IsValidHostileTarget(targetCreature, warlock);
    }

    private static readonly HashSet<string> PactSummonResRefs =
    [
        "wlkfiend",
        "wlkfey",
        "wlkcelestial",
        "wlkaberrant",
        "wlkelemental",
        "wlkslaadred",
        "wlkslaadblue",
        "wlkslaadgreen",
        "wlkslaadgray"
    ];

    private static bool IsPactSummon(this NwCreature targetCreature) => PactSummonResRefs.Contains(targetCreature.ResRef);

    /// <summary>
    /// A custom spell resistance check for warlock invocations; should always be used in place of other checks.
    /// </summary>
    /// <returns>True if the target successfully resisted the spell, otherwise false.</returns>
    public static bool InvocationResistCheck(this NwCreature warlock, NwGameObject target, int warlockLevel, bool isEldritchBlast)
    {
        if (isEldritchBlast)
        {
            // 5 is a magic number, but it's based on Globe of Invulnerability, which goes up to level 4
            const int eldritchBlastSpellLevel = 5;
            if (warlock.SpellAbsorptionLimitedCheck(target, spellSchool: SpellSchool.Unknown, spellLevel: eldritchBlastSpellLevel)
                || warlock.SpellAbsorptionUnlimitedCheck(target, spellSchool: SpellSchool.Unknown, spellLevel: eldritchBlastSpellLevel)
                || warlock.SpellImmunityCheck(target))
                return true;
        }
        else if (warlock.SpellAbsorptionLimitedCheck(target, spellSchool: SpellSchool.Unknown)
            || warlock.SpellAbsorptionUnlimitedCheck(target, spellSchool: SpellSchool.Unknown)
            || warlock.SpellImmunityCheck(target))
            return true;

        // because we want to bound warlock CL to warlock levels for immutability, we add spell pen feats separately
        // for the spell resist check
        if (warlock.KnowsFeat(Feat.EpicSpellPenetration!))
            warlockLevel += 6;
        else if (warlock.KnowsFeat(Feat.GreaterSpellPenetration!))
            warlockLevel += 4;
        else if (warlock.KnowsFeat(Feat.SpellPenetration!))
            warlockLevel += 2;

        return warlock.SpellResistanceCheck(target, casterLevel: warlockLevel);
    }
}
