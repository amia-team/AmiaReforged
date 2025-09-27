using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

public class DancingPlague
{
    private static readonly IntPtr PartyVfx = EffectVisualEffect(VFX_DUR_PIXIEDUST, FALSE, 1.4f);

    public void CastDancingPlague(uint nwnObjectId)
    {
        // Declaring variables for the damage part of the spell
        uint caster = nwnObjectId;
        uint target = GetSpellTargetObject();
        int warlockLevels = GetLevelByClass(57, caster);
        float effectDuration = warlockLevels < 10 ? RoundsToSeconds(1) : RoundsToSeconds(warlockLevels / 10);
        float delay = NwEffects.RandomFloat(1.1f, 1.5f);
        IntPtr location = GetSpellTargetLocation();

        // Declaring variables for the summon part of the spell
        float summonDuration = RoundsToSeconds(SummonUtility.PactSummonDuration(caster));
        float summonCooldown = TurnsToSeconds(1);
        IntPtr cooldownEffect = TagEffect(ExtraordinaryEffect(EffectVisualEffect(VFX_NONE)),
            sNewTag: "wlk_summon_cd");

        if (NwEffects.IsPolymorphed(nwnObjectId))
        {
            SendMessageToPC(nwnObjectId, szMessage: "You cannot cast while polymorphed.");
            return;
        }

        if (!NwEffects.IsValidSpellTarget(target, 2, caster)) return;

        //---------------------------
        // * SUMMONING
        //---------------------------

        // If summonCooldown is active, don't summon; else summon and set summonCooldown
        if (NwEffects.GetHasEffectByTag(effectTag: "wlk_summon_cd", caster) == FALSE)
        {
            // Apply cooldown
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, cooldownEffect, caster, summonCooldown);
            // Summon new
            ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_FNF_SMOKE_PUFF), location, 2f);
            ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, EffectSummonCreature(sCreatureResref: "wlkfey", -1, 1),
                location, summonDuration);
            // Apply effects
            DelayCommand(1.1f, () => SummonUtility.SetSummonsFacing(1, location));
            DelayCommand(1.1f, () => MakePretty(target));
        }

        //---------------------------
        // * HOSTILE SPELL EFFECT
        //---------------------------

        SignalEvent(target, EventSpellCastAt(caster, 1010));

        // If the target succeeds the will save or resists spell, then cancel the dance rave, boo!
        if (GetIsImmune(target, IMMUNITY_TYPE_DISEASE) == TRUE ||
            GetLocalInt(target, sVarName: "has_danced") == TRUE) return;

        bool passedFortSave =
            FortitudeSave(target, WarlockUtils.CalculateDc(caster), SAVING_THROW_TYPE_DISEASE, caster) == TRUE;

        if (passedFortSave)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FORTITUDE_SAVING_THROW_USE), target);
            return;
        }

        // If the target fails the will save, then START THE DANCE RAVE!
        if (!passedFortSave)
        {
            DelayedMakeDance(delay, target, effectDuration);
            DelayCommand(delay, () => DanceParty(caster, effectDuration, location));
        }
    }

    // This function loops the dance party effect in a colossal area for as long as creatures keep failing the will save.
    private void DanceParty(uint caster, float effectDuration, IntPtr location)
    {
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_MEDIUM, location);

        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            float delay = NwEffects.RandomFloat(0.5f, 1.5f);

            if (NwEffects.IsValidSpellTarget(currentTarget, 2, caster))
            {
                if (GetIsImmune(currentTarget, IMMUNITY_TYPE_DISEASE) == TRUE ||
                    GetLocalInt(currentTarget, sVarName: "has_danced") == TRUE)
                {
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_MEDIUM, location);
                    continue;
                }

                bool passedFortSave = FortitudeSave(currentTarget, WarlockUtils.CalculateDc(caster),
                    SAVING_THROW_TYPE_DISEASE, caster) == TRUE;

                if (passedFortSave)
                {
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FORTITUDE_SAVING_THROW_USE),
                        currentTarget);
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_MEDIUM, location);
                    continue;
                }

                if (!passedFortSave)
                {
                    DelayedMakeDance(delay, currentTarget, effectDuration);
                    IntPtr newLocation = GetLocation(currentTarget);
                    DelayedDanceParty(delay, caster, effectDuration, newLocation);
                }
            }

            currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_MEDIUM, location);
        }
    }

    private void MakeDance(uint target, float effectDuration)
    {
        ClearAllActions(TRUE);
        PlayAnimation(RandomDance(), 1, effectDuration);
        SetCommandable(FALSE, target);
    }

    private void DelayedMakeDance(float delay, uint target, float effectDuration)
    {
        DelayCommand(delay, () => SetLocalInt(target, sVarName: "has_danced", 1));
        DelayCommand(delay, () => ApplyEffectToObject(DURATION_TYPE_TEMPORARY, PartyVfx, target, effectDuration));
        DelayCommand(delay, () => AssignCommand(target, () => MakeDance(target, effectDuration)));
        DelayCommand(delay + effectDuration, () => SetCommandable(TRUE, target));
        DelayCommand(delay + effectDuration, () => DeleteLocalInt(target, sVarName: "has_danced"));
    }

    private void DelayedDanceParty(float delay, uint caster, float effectDuration, IntPtr location)
    {
        DelayCommand(delay, () => DanceParty(caster, effectDuration, location));
    }

    // Randomize dance effect... sooooo random!
    private int RandomDance()
    {
        int randomDance = d2();
        switch (randomDance)
        {
            case 1:
                return ANIMATION_LOOPING_SPASM;
            case 2:
                return ANIMATION_LOOPING_CONJURE2;
        }

        return randomDance;
    }

    private void MakePretty(uint target)
    {
        uint summon = GetAssociate(ASSOCIATE_TYPE_SUMMONED, OBJECT_SELF);

        int gender = GetGender(target);
        int appearance = GetAppearanceType(target);
        int soundset = GetSoundset(target);
        float height = GetObjectVisualTransform(target, OBJECT_VISUAL_TRANSFORM_SCALE);
        int tail = GetCreatureTailType(target);
        int wings = GetCreatureWingType(target);
        int rfoot = GetCreatureBodyPart(CREATURE_PART_RIGHT_FOOT, target);
        int lfoot = GetCreatureBodyPart(CREATURE_PART_LEFT_FOOT, target);
        int rshin = GetCreatureBodyPart(CREATURE_PART_RIGHT_SHIN, target);
        int lshin = GetCreatureBodyPart(CREATURE_PART_LEFT_SHIN, target);
        int rthigh = GetCreatureBodyPart(CREATURE_PART_RIGHT_THIGH, target);
        int lthigh = GetCreatureBodyPart(CREATURE_PART_LEFT_THIGH, target);
        int pelvis = GetCreatureBodyPart(CREATURE_PART_PELVIS, target);
        int torso = GetCreatureBodyPart(CREATURE_PART_TORSO, target);
        int belt = GetCreatureBodyPart(CREATURE_PART_BELT, target);
        int neck = GetCreatureBodyPart(CREATURE_PART_NECK, target);
        int rfore = GetCreatureBodyPart(CREATURE_PART_RIGHT_FOREARM, target);
        int lfore = GetCreatureBodyPart(CREATURE_PART_LEFT_FOREARM, target);
        int rbicep = GetCreatureBodyPart(CREATURE_PART_RIGHT_BICEP, target);
        int lbicep = GetCreatureBodyPart(CREATURE_PART_LEFT_BICEP, target);
        int rshoulder = GetCreatureBodyPart(CREATURE_PART_RIGHT_SHOULDER, target);
        int lshoulder = GetCreatureBodyPart(CREATURE_PART_LEFT_SHOULDER, target);
        int rhand = GetCreatureBodyPart(CREATURE_PART_RIGHT_HAND, target);
        int lhand = GetCreatureBodyPart(CREATURE_PART_LEFT_HAND, target);
        int head = GetCreatureBodyPart(CREATURE_PART_HEAD, target);
        int colorHair = GetColor(target, COLOR_CHANNEL_HAIR);
        int colorSkin = GetColor(target, COLOR_CHANNEL_SKIN);
        int colorTattoo1 = GetColor(target, COLOR_CHANNEL_TATTOO_1);
        int colorTattoo2 = GetColor(target, COLOR_CHANNEL_TATTOO_2);

        SetGender(summon, gender);
        SetCreatureAppearanceType(summon, appearance);
        SetSoundset(summon, soundset);
        SetObjectVisualTransform(summon, OBJECT_VISUAL_TRANSFORM_SCALE, height);
        SetCreatureWingType(wings, summon);
        SetCreatureTailType(tail, summon);
        SetCreatureBodyPart(CREATURE_PART_RIGHT_FOOT, rfoot, summon);
        SetCreatureBodyPart(CREATURE_PART_LEFT_FOOT, lfoot, summon);
        SetCreatureBodyPart(CREATURE_PART_RIGHT_SHIN, rshin, summon);
        SetCreatureBodyPart(CREATURE_PART_LEFT_SHIN, lshin, summon);
        SetCreatureBodyPart(CREATURE_PART_RIGHT_THIGH, rthigh, summon);
        SetCreatureBodyPart(CREATURE_PART_LEFT_THIGH, lthigh, summon);
        SetCreatureBodyPart(CREATURE_PART_PELVIS, pelvis, summon);
        SetCreatureBodyPart(CREATURE_PART_TORSO, torso, summon);
        SetCreatureBodyPart(CREATURE_PART_BELT, belt, summon);
        SetCreatureBodyPart(CREATURE_PART_NECK, neck, summon);
        SetCreatureBodyPart(CREATURE_PART_RIGHT_FOREARM, rfore, summon);
        SetCreatureBodyPart(CREATURE_PART_LEFT_FOREARM, lfore, summon);
        SetCreatureBodyPart(CREATURE_PART_RIGHT_BICEP, rbicep, summon);
        SetCreatureBodyPart(CREATURE_PART_LEFT_BICEP, lbicep, summon);
        SetCreatureBodyPart(CREATURE_PART_RIGHT_SHOULDER, rshoulder, summon);
        SetCreatureBodyPart(CREATURE_PART_LEFT_SHOULDER, lshoulder, summon);
        SetCreatureBodyPart(CREATURE_PART_RIGHT_HAND, rhand, summon);
        SetCreatureBodyPart(CREATURE_PART_LEFT_HAND, lhand, summon);
        SetCreatureBodyPart(CREATURE_PART_HEAD, head, summon);
        SetColor(summon, COLOR_CHANNEL_HAIR, colorHair);
        SetColor(summon, COLOR_CHANNEL_SKIN, colorSkin);
        SetColor(summon, COLOR_CHANNEL_TATTOO_1, colorTattoo1);
        SetColor(summon, COLOR_CHANNEL_TATTOO_2, colorTattoo2);
        AssignCommand(summon, () => PlayAnimation(RandomDance(), 1, 6f));
    }
}
