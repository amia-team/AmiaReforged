using AmiaReforged.Classes.Spells;
using AmiaReforged.Classes.Spells.Invocations.Dark;
using AmiaReforged.Classes.Warlock.EldritchBlast.Essence;
using AmiaReforged.Classes.Warlock.Feats;
using AmiaReforged.Classes.Warlock.Types;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.Classes.Warlock;

public static class WarlockExtensions
{
    private const VfxType SpellFailHeadVfx = (VfxType)292;
    private const VfxType SpellFailHandVfx = (VfxType)293;
    public const int WarlockId = 57;
    private const int WordOfChangingId = 994;
    public const string EldritchBlastImpactScript = "wlk_el_blst";

    public static int WarlockLevel(this NwCreature warlock) => NWScript.GetLevelByClass(WarlockId, warlock);

    public static int GetInvocationCasterLevel(this NwCreature warlock)
    {
        int casterLevel = warlock.WarlockLevel();

        if (warlock.ActiveEffects.Any(e => e.Tag == nameof(WordOfChanging)))
        {
            casterLevel -= 5;
        }

        LocalVariableInt gluttonousEssence = warlock.GetObjectVariable<LocalVariableInt>(nameof(EssenceType.Gluttonous));

        if (gluttonousEssence.HasValue)
        {
            if (gluttonousEssence.Value < 0)
                gluttonousEssence.Value = 0;

            if (gluttonousEssence.Value > 0)
            {
                if (gluttonousEssence.Value > 3) gluttonousEssence.Value = 3;
                casterLevel += gluttonousEssence.Value;
            }
        }

        return casterLevel;
    }

    public static int InvocationDc(this NwCreature warlock, int invocationCl) =>
        10 + warlock.GetAbilityModifier(Ability.Charisma) + invocationCl / 3;

    /// <summary>
    /// Harmful invocations never hurt pact summons, but otherwise respect normal targeting rules.
    /// </summary>
    /// <returns>True if the spell should hit the target, false otherwise</returns>
    public static bool IsValidInvocationTarget(this NwCreature targetCreature, NwCreature warlock, bool hurtSelf = true)
    {
        if (targetCreature.IsPactSummon() || !hurtSelf && targetCreature == warlock) return false;

        return SpellUtils.IsValidHostileTarget(targetCreature, warlock);
    }

    public static bool IsPactSummon(this NwCreature targetCreature) => targetCreature.ResRef.StartsWith("wlk");

    /// <summary>
    /// A custom spell resistance check for warlock invocations; should always be used in place of other checks.
    /// </summary>
    /// <returns>True if the target successfully resisted the spell, otherwise false.</returns>
    public static bool InvocationResistCheck(this NwCreature warlock, NwGameObject target, int invocationCl,
        bool isEldritchBlast = false)
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
            invocationCl += 6;
        else if (warlock.KnowsFeat(Feat.GreaterSpellPenetration!))
            invocationCl += 4;
        else if (warlock.KnowsFeat(Feat.SpellPenetration!))
            invocationCl += 2;

        return warlock.SpellResistanceCheck(target, casterLevel: invocationCl);
    }

    /// <summary>
    /// Does an arcane spell failure check and plays the fail vfx if the spell fails. You still need to return
    /// the actual spell early before it's cast based on the bool, though.
    /// </summary>
    /// <param name="warlock">The warlock casting the spell</param>
    /// <param name="spell">Spell being cast</param>
    /// <returns>True if spell passes asf check, false if spell fails</returns>
    public static bool CheckArcaneSpellFailure(this NwCreature warlock, NwSpell spell)
    {
        // If there's no asf at all, the spell always passes
        if (warlock.ArcaneSpellFailure <= 0) return true;

        // Check if warlock's Armored Caster feat reduces the asf check, pass spell if effective asf is 0 or lower
        int effectiveAsf = ArmoredCaster.CalculateAsf(warlock);
        if (effectiveAsf <= 0) return true;

        // Do the actual asf check
        if (effectiveAsf < Random.Shared.Roll(100)) return true;

        // Play the fail vfx
        VfxType spellFailVfx = spell.CastAnim == SpellCastAnimType.Up ? SpellFailHeadVfx : SpellFailHandVfx;
        warlock.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(spellFailVfx));

        warlock.ControllingPlayer?.SendServerMessage("Invocation failed due to arcane spell failure!");

        return false;
    }

    public static string ColorWarlock(this string message) => message.ColorString(ColorConstants.Magenta);

    public static TimeSpan PactSummonDuration(int invocationCl) => NwTimeSpan.FromRounds(5 + invocationCl / 2);

    private static TimeSpan PactSummonCooldown => NwTimeSpan.FromTurns(1);

    private const string PactSummonCooldownTag = "wlk_summon_cd";

    public static bool HasPactCooldown(this NwCreature warlock)
        => warlock.ActiveEffects.Any(e => e.Tag == PactSummonCooldownTag);

    public static void ApplyPactCooldown(this NwCreature warlock)
    {
        Effect effect = Effect.VisualEffect(VfxType.None);
        effect.SubType = EffectSubType.Extraordinary;
        effect.Tag = PactSummonCooldownTag;

        warlock.ApplyEffect(EffectDuration.Temporary, effect, PactSummonCooldown);
    }

    public static PactType? GetPact(this NwCreature warlock)
    {
        foreach (PactType pact in Enum.GetValues<PactType>())
        {
            if (warlock.Feats.Any(f => f.FeatType == (Feat)pact))
                return pact;
        }

        return null;
    }

    public static int GetFirstWarlockLevel(this NwCreature warlock)
    {
        for (int i = 0; i < warlock.LevelInfo.Count; i++)
        {
            if (warlock.LevelInfo[i].ClassInfo.Class.Id == WarlockId)
                return i + 1;
        }

        return 0;
    }
}
