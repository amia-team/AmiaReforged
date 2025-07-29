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

    // Warlock DC helper function
    public static int CalculateDc(uint caster) =>
        GetLevelByClass(57, caster) / 3 + GetAbilityModifier(ABILITY_CHARISMA, caster) + 10;

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

    public static int DispelCheck(int dispelCl, int targetEffectCl)
    {
        int dispelCheck = d20() + dispelCl - targetEffectCl + 11;
        if (dispelCheck >= 0) return TRUE;
        return FALSE;
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

    /// <summary>
    ///     Used to set a custom shape in a script.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="item">The item that has the variables for the shape stored.</param>
    /// <param name="subString">A descriptive name for the shape; use lower case.</param>
    public static void SetCustomShape(uint target, uint item, string subString, float duration)
    {
        // A failsafe: You must set local int to 1 in the right item as "has_custom_[shape name]_shape".
        int hasCustomShape = GetLocalInt(item, "has_custom_" + subString + "_shape");
        if (hasCustomShape == FALSE) return;

        int gender = GetGender(target);
        int appearance = GetAppearanceType(target);
        int pheno = GetPhenoType(target);
        int soundset = GetSoundset(target);
        int portrait = GetPortraitId(target);
        int tail = GetCreatureTailType(target);
        int wings = GetCreatureWingType(target);
        int rFoot = GetCreatureBodyPart(CREATURE_PART_RIGHT_FOOT, target);
        int lFoot = GetCreatureBodyPart(CREATURE_PART_LEFT_FOOT, target);
        int rShin = GetCreatureBodyPart(CREATURE_PART_RIGHT_SHIN, target);
        int lShin = GetCreatureBodyPart(CREATURE_PART_LEFT_SHIN, target);
        int rThigh = GetCreatureBodyPart(CREATURE_PART_RIGHT_THIGH, target);
        int lThigh = GetCreatureBodyPart(CREATURE_PART_LEFT_THIGH, target);
        int pelvis = GetCreatureBodyPart(CREATURE_PART_PELVIS, target);
        int torso = GetCreatureBodyPart(CREATURE_PART_TORSO, target);
        int belt = GetCreatureBodyPart(CREATURE_PART_BELT, target);
        int neck = GetCreatureBodyPart(CREATURE_PART_NECK, target);
        int rFore = GetCreatureBodyPart(CREATURE_PART_RIGHT_FOREARM, target);
        int lFore = GetCreatureBodyPart(CREATURE_PART_LEFT_FOREARM, target);
        int rBicep = GetCreatureBodyPart(CREATURE_PART_RIGHT_BICEP, target);
        int lBicep = GetCreatureBodyPart(CREATURE_PART_LEFT_BICEP, target);
        int rShoulder = GetCreatureBodyPart(CREATURE_PART_RIGHT_SHOULDER, target);
        int lShoulder = GetCreatureBodyPart(CREATURE_PART_LEFT_SHOULDER, target);
        int rHand = GetCreatureBodyPart(CREATURE_PART_RIGHT_HAND, target);
        int lHand = GetCreatureBodyPart(CREATURE_PART_LEFT_HAND, target);
        int head = GetCreatureBodyPart(CREATURE_PART_HEAD, target);
        int colorHair = GetColor(target, COLOR_CHANNEL_HAIR);
        int colorSkin = GetColor(target, COLOR_CHANNEL_SKIN);
        int colorTattoo1 = GetColor(target, COLOR_CHANNEL_TATTOO_1);
        int colorTattoo2 = GetColor(target, COLOR_CHANNEL_TATTOO_2);
        float scale = GetObjectVisualTransform(target, OBJECT_VISUAL_TRANSFORM_SCALE);

        SetLocalInt(item, sVarName: "original_gender", gender);
        SetLocalInt(item, sVarName: "original_pheno", appearance);
        SetLocalInt(item, sVarName: "original_appearance", pheno);
        SetLocalInt(item, sVarName: "original_soundset", soundset);
        SetLocalInt(item, sVarName: "original_portrait", portrait);
        SetLocalInt(item, sVarName: "original_tail", tail);
        SetLocalInt(item, sVarName: "original_wings", wings);
        SetLocalInt(item, sVarName: "original_rfoot", rFoot);
        SetLocalInt(item, sVarName: "original_lfoot", lFoot);
        SetLocalInt(item, sVarName: "original_rshin", rShin);
        SetLocalInt(item, sVarName: "original_lshin", lShin);
        SetLocalInt(item, sVarName: "original_rthigh", rThigh);
        SetLocalInt(item, sVarName: "original_lthigh", lThigh);
        SetLocalInt(item, sVarName: "original_pelvis", pelvis);
        SetLocalInt(item, sVarName: "original_torso", torso);
        SetLocalInt(item, sVarName: "original_belt", belt);
        SetLocalInt(item, sVarName: "original_neck", neck);
        SetLocalInt(item, sVarName: "original_rfore", rFore);
        SetLocalInt(item, sVarName: "original_lfore", lFore);
        SetLocalInt(item, sVarName: "original_rbicep", rBicep);
        SetLocalInt(item, sVarName: "original_lbicep", lBicep);
        SetLocalInt(item, sVarName: "original_rshoulder", rShoulder);
        SetLocalInt(item, sVarName: "original_lshoulder", lShoulder);
        SetLocalInt(item, sVarName: "original_rhand", rHand);
        SetLocalInt(item, sVarName: "original_lhand", lHand);
        SetLocalInt(item, sVarName: "original_head", head);
        SetLocalInt(item, sVarName: "original_colorhair", colorHair);
        SetLocalInt(item, sVarName: "original_colorSkin", colorSkin);
        SetLocalInt(item, sVarName: "original_colortattoo1", colorTattoo1);
        SetLocalInt(item, sVarName: "original_colortattoo2", colorTattoo2);
        SetLocalFloat(item, sVarName: "original_scale", scale);

        int customGender = GetLocalInt(item, subString + "_gender") + 1;
        int customAppearance = GetLocalInt(item, subString + "_appearance") + 1;
        int customPheno = GetLocalInt(item, subString + "_pheno") + 1;
        int customSoundset = GetLocalInt(item, subString + "_soundset") + 1;
        int customPortrait = GetLocalInt(item, subString + "_portrait") + 1;
        int customTail = GetLocalInt(item, subString + "_tail") + 1;
        int customWings = GetLocalInt(item, subString + "_wings") + 1;
        int customRFoot = GetLocalInt(item, subString + "_rfoot") + 1;
        int customLFoot = GetLocalInt(item, subString + "_lfoot") + 1;
        int customRShin = GetLocalInt(item, subString + "_rshin") + 1;
        int customLShin = GetLocalInt(item, subString + "_lshin") + 1;
        int customRThigh = GetLocalInt(item, subString + "rthigh") + 1;
        int customLThigh = GetLocalInt(item, subString + "_lthigh") + 1;
        int customPelvis = GetLocalInt(item, subString + "_pelvis") + 1;
        int customTorso = GetLocalInt(item, subString + "_torso") + 1;
        int customBelt = GetLocalInt(item, subString + "_belt") + 1;
        int customNeck = GetLocalInt(item, subString + "_neck") + 1;
        int customRFore = GetLocalInt(item, subString + "_rfore") + 1;
        int customLFore = GetLocalInt(item, subString + "_lfore") + 1;
        int customRBicep = GetLocalInt(item, subString + "_rbicep") + 1;
        int customLBicep = GetLocalInt(item, subString + "_lbicep") + 1;
        int customRShoulder = GetLocalInt(item, subString + "_rshoulder") + 1;
        int customLShoulder = GetLocalInt(item, subString + "_lshoulder") + 1;
        int customRHand = GetLocalInt(item, subString + "_rhand") + 1;
        int customLHand = GetLocalInt(item, subString + "_lhand") + 1;
        int customHead = GetLocalInt(item, subString + "_head") + 1;
        int customColorHair = GetLocalInt(item, subString + "_colorhair") + 1;
        int customColorSkin = GetLocalInt(item, subString + "_colorskin") + 1;
        int customColorTattoo1 = GetLocalInt(item, subString + "_colortattoo1") + 1;
        int customColorTattoo2 = GetLocalInt(item, subString + "_colortattoo2") + 1;
        float customScale = GetLocalFloat(item, subString + "_scale");

        if (customGender != 0)
            SetGender(target, customGender--);
        if (customAppearance != 0)
            SetCreatureAppearanceType(target, customAppearance--);
        if (customPheno != 0)
            SetPhenoType(customPheno--, target);
        if (customSoundset != 0)
            SetSoundset(target, customSoundset--);
        if (customPortrait != 0)
            SetPortraitId(target, customPortrait--);
        if (customTail != 0)
            SetCreatureTailType(customTail--, target);
        if (customWings != 0)
            SetCreatureWingType(customWings--, target);
        if (customRFoot != 0)
            SetCreatureBodyPart(CREATURE_PART_RIGHT_FOOT, customRFoot--, target);
        if (customLFoot != 0)
            SetCreatureBodyPart(CREATURE_PART_LEFT_FOOT, customLFoot--, target);
        if (customRShin != 0)
            SetCreatureBodyPart(CREATURE_PART_RIGHT_SHIN, customRShin--, target);
        if (customLShin != 0)
            SetCreatureBodyPart(CREATURE_PART_LEFT_SHIN, customLShin--, target);
        if (customRThigh != 0)
            SetCreatureBodyPart(CREATURE_PART_RIGHT_THIGH, customRThigh--, target);
        if (customLThigh != 0)
            SetCreatureBodyPart(CREATURE_PART_LEFT_THIGH, customLThigh--, target);
        if (customPelvis != 0)
            SetCreatureBodyPart(CREATURE_PART_PELVIS, customPelvis--, target);
        if (customTorso != 0)
            SetCreatureBodyPart(CREATURE_PART_TORSO, customTorso--, target);
        if (customBelt != 0)
            SetCreatureBodyPart(CREATURE_PART_BELT, customBelt--, target);
        if (customNeck != 0)
            SetCreatureBodyPart(CREATURE_PART_NECK, customNeck--, target);
        if (customRFore != 0)
            SetCreatureBodyPart(CREATURE_PART_RIGHT_FOREARM, customRFore--, target);
        if (customLFore != 0)
            SetCreatureBodyPart(CREATURE_PART_LEFT_FOREARM, customLFore--, target);
        if (customRBicep != 0)
            SetCreatureBodyPart(CREATURE_PART_RIGHT_BICEP, customRBicep--, target);
        if (customLBicep != 0)
            SetCreatureBodyPart(CREATURE_PART_LEFT_BICEP, customLBicep--, target);
        if (customRShoulder != 0)
            SetCreatureBodyPart(CREATURE_PART_RIGHT_SHOULDER, customRShoulder--, target);
        if (customLShoulder != 0)
            SetCreatureBodyPart(CREATURE_PART_LEFT_SHOULDER, customLShoulder--, target);
        if (customRHand != 0)
            SetCreatureBodyPart(CREATURE_PART_RIGHT_HAND, customRHand--, target);
        if (customLHand != 0)
            SetCreatureBodyPart(CREATURE_PART_LEFT_HAND, customLHand--, target);
        if (customHead != 0)
            SetCreatureBodyPart(CREATURE_PART_HEAD, customHead--, target);
        if (customColorHair != 0)
            SetColor(target, COLOR_CHANNEL_HAIR, customColorHair--);
        if (customColorSkin != 0)
            SetColor(target, COLOR_CHANNEL_SKIN, customColorSkin--);
        if (customColorTattoo1 != 0)
            SetColor(target, COLOR_CHANNEL_TATTOO_1, customColorTattoo1--);
        if (customColorTattoo2 != 0)
            SetColor(target, COLOR_CHANNEL_TATTOO_2, customColorTattoo2--);
        if (customScale != 0.0f)
            SetObjectVisualTransform(target, OBJECT_VISUAL_TRANSFORM_SCALE, customScale);

        // Set a custom shape effect that is used to control the deshaping
        IntPtr customShapeEffect = EffectVisualEffect(VFX_DUR_CESSATE_NEUTRAL);
        customShapeEffect = TagEffect(customShapeEffect, sNewTag: "customshape_effect");
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, customShapeEffect, target, duration);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_POLYMORPH), target);
    }

    public static void DoBreach(NwObject target, int breachAmount, int? spellResistanceDecrease = null, Spell? sourceSpell = null)
    {
        NwCreature creature = (NwCreature)target;

        List<Spell> breachList = BreachList.BreachSpells;
        List<Spell> breachableSpellEffects = new();
        
        foreach (Effect effect in creature.ActiveEffects)
        {
            if (effect.Spell is null) continue;
            
            breachableSpellEffects.Add(effect.Spell.SpellType);
        }

        breachableSpellEffects = breachList.Intersect(breachableSpellEffects).ToList();

        if (breachableSpellEffects.Count == 0) return;

        for (int i = 0; i < breachAmount; i++)
        {
            Spell spell = breachableSpellEffects[i];
            Effect? effectToBreach = creature.ActiveEffects.FirstOrDefault(effect => effect.Spell!.SpellType == spell);

            if (effectToBreach == null) return;
            
            creature.RemoveEffect(effectToBreach);
            creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpBreach));
        }

    }
}