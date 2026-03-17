using AmiaReforged.Classes.Spells;
using Anvil.API;
using NWN.Core;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.EffectUtils;

public static class NwEffects
{
    private const int SpellTargetAllallies = 1;
    private const int SpellTargetStandardhostile = 2;
    private const int SpellTargetSelectivehostile = 3;

    /// <summary>
    ///     Links all effects in a provided list.
    /// </summary>
    /// <param name="effects">The list of effects to link together</param>
    /// <returns>A single effect that has linked all given effects</returns>
    public static IntPtr LinkEffectList(IReadOnlyList<IntPtr> effects)
    {
        IntPtr linkedEffects = EffectLinkEffects(effects[0], effects[1]);

        return effects.Where(effect => effect != effects[0] && effect != effects[1])
            .Aggregate(linkedEffects, EffectLinkEffects);
    }

    /// <summary>
    ///     Removes area of effects of a given tag belonging to a specific caster by emitting a sphere with a colossal radius
    ///     and searching for AoE effects within that shape.
    /// </summary>
    /// <param name="location">Where the sphere will be emitted from</param>
    /// <param name="caster">The person who created the AoE being searched for</param>
    /// <param name="aoeTag">The tag of the AoE in question, sourced from vfx_persistent.2da</param>
    /// <param name="radiusSize"></param>
    public static void RemoveAoeWithTag(IntPtr location, uint caster, string aoeTag, float radiusSize)
    {
        uint currentObject =
            GetFirstObjectInShape(SHAPE_SPHERE, radiusSize, location,
                FALSE, OBJECT_TYPE_AREA_OF_EFFECT);

        while (GetIsObjectValid(currentObject) == TRUE)
        {
            if (GetAreaOfEffectCreator(currentObject) != caster)
            {
                currentObject =
                    GetNextObjectInShape(SHAPE_SPHERE, radiusSize, location,
                        FALSE,
                        OBJECT_TYPE_AREA_OF_EFFECT);
                continue;
            }

            if (GetTag(currentObject) == aoeTag) DestroyObject(currentObject);

            currentObject = GetNextObjectInShape(SHAPE_SPHERE, radiusSize,
                location, FALSE,
                OBJECT_TYPE_AREA_OF_EFFECT);
        }
    }

    /// <summary>
    ///     Determines whether or not a creature is polymorphed.
    /// </summary>
    /// <param name="nwnObjectId">Object ID of the creature under scrutiny.</param>
    /// <returns></returns>
    public static bool IsPolymorphed(uint nwnObjectId)
    {
        IntPtr effect = GetFirstEffect(nwnObjectId);

        bool poly = false;
        while (GetIsEffectValid(effect) == TRUE)
        {
            if (GetEffectType(effect) == EFFECT_TYPE_POLYMORPH)
            {
                poly = true;
                break;
            }

            effect = GetNextEffect(nwnObjectId);
        }

        return poly;
    }

    /// <summary>
    ///     C# implementation of MyResistSpell that is infinitely less convoluted.
    /// </summary>
    /// <param name="caster">The creature that cast the spell.</param>
    /// <param name="target">The creature that is the  target of the spell.</param>
    public static bool ResistSpell(uint caster, uint target)
    {
        int resistSpell = NWScript.ResistSpell(caster, target);

        switch (resistSpell)
        {
            case 1 or 2:
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_DUR_MAGIC_RESISTANCE), target);
                break;
            case 3:
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_SPELL_MANTLE_USE), target);
                break;
        }

        return resistSpell > 0;
    }

    public static bool HasMantle(uint target) =>
        GetHasSpellEffect(SPELL_LESSER_SPELL_MANTLE, target) == TRUE ||
        GetHasSpellEffect(SPELL_SPELL_MANTLE, target) == TRUE ||
        GetHasSpellEffect(SPELL_GREATER_SPELL_MANTLE, target) == TRUE;

    public static int GetHasEffectType(int effectType, uint creature)
    {
        IntPtr effect = GetFirstEffect(creature);
        while (GetIsEffectValid(effect) == TRUE)
        {
            if (GetEffectType(effect) == effectType) return TRUE;
            effect = GetNextEffect(creature);
        }

        return FALSE;
    }

    public static int GetHasEffectByTag(string effectTag, uint creature)
    {
        IntPtr effect = GetFirstEffect(creature);
        while (GetIsEffectValid(effect) == TRUE)
        {
            if (GetEffectTag(effect) == effectTag) return TRUE;
            effect = GetNextEffect(creature);
        }

        return FALSE;
    }

    public static void RemoveEffectByTag(string tag, uint creature)
    {
        IntPtr effect = GetFirstEffect(creature);
        while (GetIsEffectValid(effect) == TRUE)
        {
            if (GetEffectTag(effect) == tag) RemoveEffect(creature, effect);
            effect = GetNextEffect(creature);
        }
    }

    public static float RandomFloat(float minFloat, float maxFloat)
    {
        float floatRange = (maxFloat - minFloat) * 100;
        float randomInt = Random(FloatToInt(floatRange)) + 1;
        return randomInt / 100 + minFloat;
    }

    public static int GetXpForLevel(int level) => level * (level - 1) * 500;

    public static int GetEffectCount(uint target)
    {
        int effectCount = 0;
        IntPtr effect = GetFirstEffect(target);
        while (GetIsEffectValid(effect) == TRUE)
        {
            effectCount++;
            effect = GetNextEffect(target);
        }

        return effectCount;
    }

    public static IntPtr GetHighestClMagicEffect(uint target)
    {
        int effectCount = GetEffectCount(target);

        if (effectCount > 0)
        {
            IntPtr effect = GetFirstEffect(target);
            IntPtr compareEffect = GetNextEffect(target);

            for (int i = 0; GetEffectSubType(effect) == SUBTYPE_MAGICAL && i <= effectCount; i++)
            {
                if (GetEffectCasterLevel(effect) < GetEffectCasterLevel(compareEffect))
                    compareEffect = GetNextEffect(target);
                else
                    effect = GetNextEffect(target);
            }

            if (GetEffectCasterLevel(effect) <= GetEffectCasterLevel(compareEffect)) return compareEffect;
            if (GetEffectCasterLevel(compareEffect) <= GetEffectCasterLevel(effect)) return effect;
        }

        return default;
    }


    public static bool IsValidSpellTarget(uint target, int targetType, uint caster)
    {
        // if dead, not a valid target
        if (GetIsDead(target) == TRUE) return false;

        // if DM, not a valid target
        if (GetIsDM(target) == TRUE) return false;

        bool returnBool = false;

        switch (targetType)
        {
            // * this kind of spell will affect all friendlies and anyone in my
            // * party, even if we are upset with each other currently.
            case SpellTargetAllallies:
            {
                if (GetIsReactionTypeFriendly(target, caster) == TRUE || GetFactionEqual(target, caster) == TRUE)
                    returnBool = true;
                break;
            }
            case SpellTargetStandardhostile:
            {
                //SpawnScriptDebugger();
                bool isPc = GetIsPC(target) == TRUE;
                bool isNotFriend = GetIsReactionTypeFriendly(target, caster) == FALSE;

                // * Local Override is just an out for end users who want
                // * the area effect spells to hurt 'neutrals'
                if (GetLocalInt(GetModule(), sVarName: "X0_G_ALLOWSPELLSTOHURT") == 10) isPc = true;

                int isSelfTarget = FALSE;
                uint master = GetMaster(target);

                // March 25 2003. The player itself can be harmed by their own area of effect spells if in Hardcore mode...
                if (GetGameDifficulty() > GAME_DIFFICULTY_NORMAL)
                {
                    // Have I hit myself with my spell?
                    if (target == caster) isSelfTarget = TRUE;
                    // * Is the  target an associate of the spellcaster
                    if (master == caster) isSelfTarget = TRUE;
                    if (master == caster && SummonUtility.IsPactSummon(target)) isSelfTarget = FALSE;
                }

                // April 9 2003
                // Hurt the associates of a hostile player
                if (isSelfTarget == FALSE && GetIsObjectValid(master) == TRUE)
                    // * I am an associate of someone
                    if (GetIsReactionTypeFriendly(master, caster) == FALSE && GetIsPC(master) == TRUE
                        || GetIsReactionTypeHostile(master, caster) == TRUE)
                        isSelfTarget = TRUE;

                // Assumption: In Full PvP players, even if in same party, are Neutral
                // * GZ: 2003-08-30: Patch to make creatures hurt each other in hardcore mode...

                if (GetIsReactionTypeHostile(target, caster) == TRUE)
                    returnBool = true; // Hostile creatures are always a target
                else if (isSelfTarget == TRUE)
                    returnBool = true; // Targetting Self (set above)?
                else if (isPc && isNotFriend)
                    returnBool = true; // Enemy PC
                else if (isNotFriend && GetGameDifficulty() > GAME_DIFFICULTY_NORMAL)
                    if (GetLocalInt(GetModule(), sVarName: "X2_SWITCH_ENABLE_NPC_AOE_HURT_ALLIES") == 1)
                        returnBool = true; // Hostile Creature and Difficulty > Normal
                // note that in hardcore mode any creature is hostile

                break;
            }
            // * only harms enemies, ever
            // * current list:call lightning, isaac missiles, firebrand, chain lightning, dirge, Nature's balance,
            // * Word of Faith
            case SpellTargetSelectivehostile:
            {
                if (GetIsEnemy(target, caster) == TRUE) returnBool = true;
                break;
            }
        }

        // GZ: Creatures with the same master will never damage each other
        if (GetMaster(target) != OBJECT_INVALID && GetMaster(caster) != OBJECT_INVALID
                                                && GetMaster(target) == GetMaster(caster)
                                                && GetLocalInt(GetModule(),
                                                    sVarName: "X2_SWITCH_ENABLE_NPC_AOE_HURT_ALLIES") == 0)
            returnBool = false;

        return returnBool;
    }

    public static string ColorString(string message, string rgb)
    {
        // The magic characters (padded -- the last three characters are the same).
        string colorCodes = " fw®°Ìþþþ";

        return "<c" + // Begin the color token.
               GetSubString(colorCodes, StringToInt(GetSubString(rgb, 0, 1)), 1) + // red
               GetSubString(colorCodes, StringToInt(GetSubString(rgb, 1, 1)), 1) + // green
               GetSubString(colorCodes, StringToInt(GetSubString(rgb, 2, 1)), 1) + // blue
               ">" + // End the color token
               message + "</c>";
    }
}
