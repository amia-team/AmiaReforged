using NUnit.Framework.Constraints;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types;

public static class EssenceVfx
{
    public static IntPtr Beam(EssenceType essence, uint source, int bodyPartNode = BODY_NODE_HAND, int miss = FALSE) =>
        essence switch
        {
            EssenceType.RemoveEssence => EffectBeam(BeamVfxConst(essence), source, bodyPartNode, miss,
                2.0f),
            EssenceType.Frightful => EffectBeam(BeamVfxConst(essence), source, bodyPartNode, miss,
                2.0f),
            EssenceType.Draining => EffectBeam(BeamVfxConst(essence), source, bodyPartNode, miss,
                2.0f),
            EssenceType.Vitriolic => EffectBeam(BeamVfxConst(essence), source,
                bodyPartNode, miss,
                2.0f),
            EssenceType.Screaming => EffectBeam(BeamVfxConst(essence), source, bodyPartNode, miss,
                2.0f),
            EssenceType.Hellrime => EffectBeam(BeamVfxConst(essence), source, bodyPartNode, miss,
                2.0f),
            EssenceType.Utterdark => EffectBeam(BeamVfxConst(essence), source, bodyPartNode, miss,
                2.0f),
            EssenceType.Brimstone => EffectBeam(BeamVfxConst(essence), source, bodyPartNode, miss,
                2.0f),
            EssenceType.Beshadowed => EffectBeam(BeamVfxConst(essence), source, bodyPartNode,
                miss,
                2.0f),
            EssenceType.Binding => EffectBeam(BeamVfxConst(essence), source, bodyPartNode, miss,
                2.0f),
            EssenceType.Bewitching => EffectBeam(BeamVfxConst(essence), source, bodyPartNode, miss,
                2.0f),
            _ => EffectBeam(BeamVfxConst(essence), source, bodyPartNode)
        };
    public static int BeamVfxConst(EssenceType essence) =>
        essence switch
        {
            EssenceType.RemoveEssence => VFX_BEAM_ODD,
            EssenceType.Frightful => VFX_BEAM_MIND,
            EssenceType.Draining => VFX_BEAM_ODD,
            EssenceType.Vitriolic => VFX_BEAM_DISINTEGRATE,
            EssenceType.Screaming => VFX_BEAM_SILENT_COLD,
            EssenceType.Hellrime => VFX_BEAM_COLD,
            EssenceType.Utterdark => VFX_BEAM_EVIL,
            EssenceType.Brimstone => VFX_BEAM_FIRE_W,
            EssenceType.Beshadowed => VFX_BEAM_BLACK,
            EssenceType.Binding => VFX_BEAM_MIND,
            EssenceType.Bewitching => VFX_BEAM_MIND,
            _ => VFX_BEAM_ODD
        };
    public static IntPtr Doom(EssenceType essence) =>
        essence switch
        {
            EssenceType.RemoveEssence => EffectVisualEffect(VFX_IMP_MAGBLUE, FALSE, 13.0f),
            EssenceType.Frightful => EffectVisualEffect(VFX_IMP_MAGBLUE, FALSE, 13.0f),
            EssenceType.Draining => EffectVisualEffect(VFX_IMP_MAGBLUE, FALSE, 13.0f),
            EssenceType.Vitriolic => EffectVisualEffect(VFX_IMP_MAGBLUE, FALSE, 13.0f),
            EssenceType.Screaming => EffectVisualEffect(VFX_FNF_SOUND_BURST, FALSE, 0.7f),
            EssenceType.Hellrime => EffectVisualEffect(VFX_IMP_FROST_L, FALSE, 4.0f),
            EssenceType.Utterdark => EffectVisualEffect(VFX_FNF_GAS_EXPLOSION_EVIL, FALSE, 6.0f),
            EssenceType.Brimstone => EffectVisualEffect(VFX_IMP_FLAME_M, FALSE, 4.0f),
            EssenceType.Beshadowed => EffectVisualEffect(VFX_FNF_GAS_EXPLOSION_GREASE, FALSE, 6.0f),
            EssenceType.Binding => EffectVisualEffect(VFX_IMP_MAGBLUE, FALSE, 13.0f),
            EssenceType.Bewitching => EffectVisualEffect(VFX_IMP_MAGBLUE, FALSE, 13.0f),
            _ => EffectVisualEffect(VFX_IMP_MAGBLUE, FALSE, 13.0f)
        };
    public static IntPtr Pulse(EssenceType essence) =>
        essence switch
        {
            EssenceType.RemoveEssence => EffectVisualEffect(VFX_IMP_MAGBLUE, FALSE, 8.5f),
            EssenceType.Frightful => EffectVisualEffect(VFX_IMP_MAGBLUE, FALSE, 8.5f),
            EssenceType.Draining => EffectVisualEffect(VFX_IMP_MAGBLUE, FALSE, 8.5f),
            EssenceType.Vitriolic => EffectVisualEffect(VFX_IMP_ACID_S, FALSE, 3f),
            EssenceType.Screaming => EffectVisualEffect(VFX_FNF_SOUND_BURST, FALSE, 0.5f),
            EssenceType.Hellrime => EffectVisualEffect(VFX_IMP_PULSE_COLD, FALSE, 0.6f),
            EssenceType.Utterdark => EffectVisualEffect(VFX_IMP_PULSE_NEGATIVE, FALSE, 0.6f),
            EssenceType.Brimstone => EffectVisualEffect(VFX_IMP_PULSE_FIRE, FALSE, 0.6f),
            EssenceType.Beshadowed => EffectVisualEffect(VFX_FNF_GAS_EXPLOSION_GREASE, FALSE, 4f),
            EssenceType.Binding => EffectVisualEffect(VFX_IMP_MAGBLUE, FALSE, 8.5f),
            EssenceType.Bewitching => EffectVisualEffect(VFX_IMP_MAGBLUE, FALSE, 8.5f),
            _ => EffectVisualEffect(VFX_IMP_MAGBLUE, FALSE, 8.5f)
        };
    public static IntPtr Mastery(EssenceType essence, uint nwnObjectId)
    {
        float scale = GetObjectVisualTransform(nwnObjectId, OBJECT_VISUAL_TRANSFORM_SCALE);

        int appearanceType = GetAppearanceType(nwnObjectId);
        int gender = GetGender(nwnObjectId);
        bool phenotypeIsNormal = GetPhenoType(nwnObjectId) == PHENOTYPE_NORMAL || 
            GetPhenoType(nwnObjectId) == PHENOTYPE_BIG;

        if (!phenotypeIsNormal)
            return essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_DUR_AURA_PURPLE),
                EssenceType.Frightful => EffectVisualEffect(VFX_DUR_AURA_PURPLE),
                EssenceType.Draining => EffectVisualEffect(VFX_DUR_AURA_PURPLE),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_DUR_AURA_GREEN),
                EssenceType.Screaming => EffectVisualEffect(VFX_DUR_AURA_WHITE),
                EssenceType.Hellrime => EffectVisualEffect(VFX_DUR_AURA_CYAN),
                EssenceType.Utterdark => EffectVisualEffect(VFX_DUR_AURA_RED_DARK),
                EssenceType.Brimstone => EffectVisualEffect(VFX_DUR_AURA_ORANGE),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_DUR_AURA_BLUE_DARK),
                EssenceType.Binding => EffectVisualEffect(VFX_DUR_AURA_PURPLE),
                EssenceType.Bewitching => EffectVisualEffect(VFX_DUR_AURA_PURPLE),
                _ => EffectVisualEffect(VFX_DUR_AURA_PURPLE)
            };

        return (appearanceType, gender)
        switch
        {
            (APPEARANCE_TYPE_DWARF, GENDER_MALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_DWARF_MALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_DWARF_MALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_DWARF_MALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_DWARF_MALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_DWARF_MALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale)
            },
            (APPEARANCE_TYPE_ELF, GENDER_MALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_ELF_MALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_ELF_MALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_ELF_MALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_ELF_MALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_ELF_MALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale)
            },
            (APPEARANCE_TYPE_GNOME, GENDER_MALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_GNOME_MALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_GNOME_MALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_GNOME_MALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_GNOME_MALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_GNOME_MALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale)
            },
            (APPEARANCE_TYPE_HALFLING, GENDER_MALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HALFLING_MALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HALFLING_MALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HALFLING_MALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HALFLING_MALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HALFLING_MALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale)
            },
            (APPEARANCE_TYPE_HALF_ELF, GENDER_MALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HUMAN_MALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HUMAN_MALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HUMAN_MALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HUMAN_MALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HUMAN_MALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale)
            },
            (APPEARANCE_TYPE_HUMAN, GENDER_MALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HUMAN_MALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HUMAN_MALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HUMAN_MALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HUMAN_MALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HUMAN_MALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale)
            },
            (APPEARANCE_TYPE_HALF_ORC, GENDER_MALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HALFORC_MALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HALFORC_MALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HALFORC_MALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HALFORC_MALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HALFORC_MALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale)
            },
            (APPEARANCE_TYPE_DWARF, GENDER_FEMALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_DWARF_FEMALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_DWARF_FEMALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_DWARF_FEMALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_DWARF_FEMALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_DWARF_FEMALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale)
            },
            (APPEARANCE_TYPE_ELF, GENDER_FEMALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_ELF_FEMALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_ELF_FEMALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_ELF_FEMALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_ELF_FEMALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_ELF_FEMALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale)
            },
            (APPEARANCE_TYPE_GNOME, GENDER_FEMALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_GNOME_FEMALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_GNOME_FEMALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_GNOME_FEMALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_GNOME_FEMALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_GNOME_FEMALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale)
            },
            (APPEARANCE_TYPE_HALFLING, GENDER_FEMALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HALFLING_FEMALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HALFLING_FEMALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HALFLING_FEMALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HALFLING_FEMALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HALFLING_FEMALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale)
            },
            (APPEARANCE_TYPE_HALF_ELF, GENDER_FEMALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale)
            },
            (APPEARANCE_TYPE_HUMAN, GENDER_FEMALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale)
            },
            (APPEARANCE_TYPE_HALF_ORC, GENDER_FEMALE) => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale),
                EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale),
                EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HALFORC_FEMALE, FALSE, scale),
                EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HALFORC_FEMALE, FALSE, scale),
                EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HALFORC_FEMALE, FALSE, scale),
                EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HALFORC_FEMALE, FALSE, scale),
                EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HALFORC_FEMALE, FALSE, scale),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale),
                EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale),
                EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale),
                _ => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale)
            },
            _ => essence switch
            {
                EssenceType.RemoveEssence => EffectVisualEffect(VFX_DUR_AURA_PURPLE),
                EssenceType.Frightful => EffectVisualEffect(VFX_DUR_AURA_PURPLE),
                EssenceType.Draining => EffectVisualEffect(VFX_DUR_AURA_PURPLE),
                EssenceType.Vitriolic => EffectVisualEffect(VFX_DUR_AURA_GREEN),
                EssenceType.Screaming => EffectVisualEffect(VFX_DUR_AURA_WHITE),
                EssenceType.Hellrime => EffectVisualEffect(VFX_DUR_AURA_CYAN),
                EssenceType.Utterdark => EffectVisualEffect(VFX_DUR_AURA_RED_DARK),
                EssenceType.Brimstone => EffectVisualEffect(VFX_DUR_AURA_ORANGE),
                EssenceType.Beshadowed => EffectVisualEffect(VFX_DUR_AURA_BLUE_DARK),
                EssenceType.Binding => EffectVisualEffect(VFX_DUR_AURA_PURPLE),
                EssenceType.Bewitching => EffectVisualEffect(VFX_DUR_AURA_PURPLE),
                _ => EffectVisualEffect(VFX_DUR_AURA_PURPLE)
            },
        };
    }
}

// UNUSED, SAVE FOR LATER TESTING
/*      bool isDwarf = GetAppearanceType(nwnObjectId) == APPEARANCE_TYPE_DWARF;
        bool isElf = GetAppearanceType(nwnObjectId) == APPEARANCE_TYPE_ELF;
        bool isGnome = GetAppearanceType(nwnObjectId) == APPEARANCE_TYPE_GNOME;
        bool isHalfling = GetAppearanceType(nwnObjectId) == APPEARANCE_TYPE_HALFLING;
        bool isHalfElf = GetAppearanceType(nwnObjectId) == APPEARANCE_TYPE_HALF_ELF;
        bool isHalfOrc = GetAppearanceType(nwnObjectId) == APPEARANCE_TYPE_HALF_ORC;
        bool isHuman = GetAppearanceType(nwnObjectId) == APPEARANCE_TYPE_HUMAN;

        bool isMale = GetGender(nwnObjectId) == GENDER_MALE;
        bool isFemale = GetGender(nwnObjectId) == GENDER_FEMALE;
        bool isValidPheno = GetPhenoType(nwnObjectId) == PHENOTYPE_NORMAL
        || GetPhenoType(nwnObjectId) == PHENOTYPE_BIG; */
/* 
        if (isValidPheno)
        {
            if (isMale)
            {
                if (isDwarf)
                {
                    return essence switch
                    {
                        EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale),
                        EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale),
                        EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale),
                        EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_DWARF_MALE, FALSE, scale),
                        EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_DWARF_MALE, FALSE, scale),
                        EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_DWARF_MALE, FALSE, scale),
                        EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_DWARF_MALE, FALSE, scale),
                        EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_DWARF_MALE, FALSE, scale),
                        EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale),
                        EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale),
                        EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale),
                        _ => EffectVisualEffect(VFX_EYES_PUR_DWARF_MALE, FALSE, scale)
                    };
                }
                if (isElf)
                {
                    return essence switch
                    {
                        EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale),
                        EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale),
                        EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale),
                        EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_ELF_MALE, FALSE, scale),
                        EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_ELF_MALE, FALSE, scale),
                        EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_ELF_MALE, FALSE, scale),
                        EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_ELF_MALE, FALSE, scale),
                        EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_ELF_MALE, FALSE, scale),
                        EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale),
                        EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale),
                        EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale),
                        _ => EffectVisualEffect(VFX_EYES_PUR_ELF_MALE, FALSE, scale)
                    };
                }
                if (isGnome)
                {
                    return essence switch
                    {
                        EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale),
                        EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale),
                        EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale),
                        EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_GNOME_MALE, FALSE, scale),
                        EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_GNOME_MALE, FALSE, scale),
                        EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_GNOME_MALE, FALSE, scale),
                        EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_GNOME_MALE, FALSE, scale),
                        EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_GNOME_MALE, FALSE, scale),
                        EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale),
                        EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale),
                        EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale),
                        _ => EffectVisualEffect(VFX_EYES_PUR_GNOME_MALE, FALSE, scale)
                    };
                }
                if (isHalfling)
                {
                    return essence switch
                    {
                        EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale),
                        EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale),
                        EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale),
                        EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HALFLING_MALE, FALSE, scale),
                        EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HALFLING_MALE, FALSE, scale),
                        EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HALFLING_MALE, FALSE, scale),
                        EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HALFLING_MALE, FALSE, scale),
                        EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HALFLING_MALE, FALSE, scale),
                        EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale),
                        EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale),
                        EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale),
                        _ => EffectVisualEffect(VFX_EYES_PUR_HALFLING_MALE, FALSE, scale)
                    };
                }
                if (isHalfElf || isHuman)
                {
                    return essence switch
                    {
                        EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                        EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                        EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                        EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HUMAN_MALE, FALSE, scale),
                        EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HUMAN_MALE, FALSE, scale),
                        EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HUMAN_MALE, FALSE, scale),
                        EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HUMAN_MALE, FALSE, scale),
                        EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HUMAN_MALE, FALSE, scale),
                        EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                        EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                        EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale),
                        _ => EffectVisualEffect(VFX_EYES_PUR_HUMAN_MALE, FALSE, scale)
                    };
                }
                if (isHalfOrc)
                {
                    return essence switch
                    {
                        EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale),
                        EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale),
                        EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale),
                        EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HALFORC_MALE, FALSE, scale),
                        EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HALFORC_MALE, FALSE, scale),
                        EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HALFORC_MALE, FALSE, scale),
                        EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HALFORC_MALE, FALSE, scale),
                        EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HALFORC_MALE, FALSE, scale),
                        EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale),
                        EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale),
                        EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale),
                        _ => EffectVisualEffect(VFX_EYES_PUR_HALFORC_MALE, FALSE, scale)
                    };
                }
            }
            if (isFemale)
            {
                if (isDwarf)
                {
                    return essence switch
                    {
                        EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale),
                        EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale),
                        EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale),
                        EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_DWARF_FEMALE, FALSE, scale),
                        EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_DWARF_FEMALE, FALSE, scale),
                        EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_DWARF_FEMALE, FALSE, scale),
                        EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_DWARF_FEMALE, FALSE, scale),
                        EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_DWARF_FEMALE, FALSE, scale),
                        EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale),
                        EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale),
                        EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale),
                        _ => EffectVisualEffect(VFX_EYES_PUR_DWARF_FEMALE, FALSE, scale)
                    };
                }
                if (isElf)
                {
                    return essence switch
                    {
                        EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale),
                        EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale),
                        EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale),
                        EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_ELF_FEMALE, FALSE, scale),
                        EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_ELF_FEMALE, FALSE, scale),
                        EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_ELF_FEMALE, FALSE, scale),
                        EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_ELF_FEMALE, FALSE, scale),
                        EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_ELF_FEMALE, FALSE, scale),
                        EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale),
                        EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale),
                        EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale),
                        _ => EffectVisualEffect(VFX_EYES_PUR_ELF_FEMALE, FALSE, scale)
                    };
                }
                if (isGnome)
                {
                    return essence switch
                    {
                        EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale),
                        EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale),
                        EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale),
                        EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_GNOME_FEMALE, FALSE, scale),
                        EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_GNOME_FEMALE, FALSE, scale),
                        EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_GNOME_FEMALE, FALSE, scale),
                        EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_GNOME_FEMALE, FALSE, scale),
                        EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_GNOME_FEMALE, FALSE, scale),
                        EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale),
                        EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale),
                        EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale),
                        _ => EffectVisualEffect(VFX_EYES_PUR_GNOME_FEMALE, FALSE, scale)
                    };
                }
                if (isHalfling)
                {
                    return essence switch
                    {
                        EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale),
                        EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale),
                        EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale),
                        EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HALFLING_FEMALE, FALSE, scale),
                        EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HALFLING_FEMALE, FALSE, scale),
                        EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HALFLING_FEMALE, FALSE, scale),
                        EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HALFLING_FEMALE, FALSE, scale),
                        EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HALFLING_FEMALE, FALSE, scale),
                        EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale),
                        EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale),
                        EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale),
                        _ => EffectVisualEffect(VFX_EYES_PUR_HALFLING_FEMALE, FALSE, scale)
                    };
                }
                if (isHalfElf || isHuman)
                {
                    return essence switch
                    {
                        EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                        EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                        EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                        EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HUMAN_FEMALE, FALSE, scale),
                        EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HUMAN_FEMALE, FALSE, scale),
                        EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HUMAN_FEMALE, FALSE, scale),
                        EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HUMAN_FEMALE, FALSE, scale),
                        EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HUMAN_FEMALE, FALSE, scale),
                        EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                        EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                        EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale),
                        _ => EffectVisualEffect(VFX_EYES_PUR_HUMAN_FEMALE, FALSE, scale)
                    };
                }
                if (isHalfOrc)
                {
                    return essence switch
                    {
                        EssenceType.RemoveEssence => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale),
                        EssenceType.Frightful => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale),
                        EssenceType.Draining => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale),
                        EssenceType.Vitriolic => EffectVisualEffect(VFX_EYES_GREEN_HALFORC_FEMALE, FALSE, scale),
                        EssenceType.Screaming => EffectVisualEffect(VFX_EYES_WHT_HALFORC_FEMALE, FALSE, scale),
                        EssenceType.Hellrime => EffectVisualEffect(VFX_EYES_CYN_HALFORC_FEMALE, FALSE, scale),
                        EssenceType.Utterdark => EffectVisualEffect(VFX_EYES_RED_FLAME_HALFORC_FEMALE, FALSE, scale),
                        EssenceType.Brimstone => EffectVisualEffect(VFX_EYES_ORG_HALFORC_FEMALE, FALSE, scale),
                        EssenceType.Beshadowed => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale),
                        EssenceType.Binding => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale),
                        EssenceType.Bewitching => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale),
                        _ => EffectVisualEffect(VFX_EYES_PUR_HALFORC_FEMALE, FALSE, scale)
                    };
                }
            }
        } */