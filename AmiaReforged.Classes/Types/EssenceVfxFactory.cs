using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types;

public static class EssenceVfxFactory
{
    public static EssenceVisuals CreateEssence(EssenceType essence, uint source, int miss = FALSE,
        int bodyPartNode = BODY_NODE_HAND)
    {
        return new EssenceVisuals(BeamVfxFromEssence(essence, source, bodyPartNode, miss),
            ImpactVfxFromEssence(essence, miss), AoeEffectFromEssence(essence), BeamVfxConst(essence));
    }

    private static IntPtr BeamVfxFromEssence(EssenceType essence, uint source, int bodyPartNode, int miss = FALSE) =>
        essence switch
        {
            EssenceType.NoEssence => EffectBeam(BeamVfxConst(essence), source, bodyPartNode, miss,
                2.0f),
            EssenceType.Frightful => EffectBeam(BeamVfxConst(essence), source, bodyPartNode, miss,
                2.0f),
            EssenceType.Draining => EffectBeam(BeamVfxConst(essence), source, bodyPartNode, miss,
                2.0f),
            EssenceType.Vitriolic => EffectBeam(BeamVfxConst(essence), source,
                bodyPartNode, miss,
                2.0f),
            EssenceType.Hindering => EffectBeam(BeamVfxConst(essence), source, bodyPartNode, miss,
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

    private static int BeamVfxConst(EssenceType essence) =>
        essence switch
        {
            EssenceType.NoEssence => VFX_BEAM_ODD,
            EssenceType.Frightful => VFX_BEAM_BLACK,
            EssenceType.Draining => VFX_BEAM_BLACK,
            EssenceType.Vitriolic => VFX_BEAM_DISINTEGRATE,
            EssenceType.Hindering => VFX_BEAM_ODD,
            EssenceType.Hellrime => VFX_BEAM_COLD,
            EssenceType.Utterdark => VFX_BEAM_EVIL,
            EssenceType.Brimstone => VFX_BEAM_FIRE_W,
            EssenceType.Beshadowed => VFX_BEAM_BLACK,
            EssenceType.Binding => VFX_BEAM_MIND,
            EssenceType.Bewitching => VFX_BEAM_MIND,
            _ => VFX_BEAM_ODD
        };

    private static IntPtr ImpactVfxFromEssence(EssenceType essence, int shouldMiss = FALSE) =>
        essence switch
        {
            EssenceType.NoEssence => EffectVisualEffect(VFX_IMP_MAGBLUE, shouldMiss, 2.0f),
            EssenceType.Frightful => EffectVisualEffect(VFX_IMP_GREASE, shouldMiss, 2.0f),
            EssenceType.Draining => EffectVisualEffect(VFX_IMP_NEGATIVE_ENERGY, shouldMiss, 2.0f),
            EssenceType.Vitriolic => EffectVisualEffect(VFX_IMP_ACID_S, shouldMiss, 2.0f),
            EssenceType.Hindering => EffectVisualEffect(VFX_IMP_SLOW, shouldMiss, 2.0f),
            EssenceType.Hellrime => EffectVisualEffect(VFX_IMP_FROST_S, shouldMiss, 2.0f),
            EssenceType.Utterdark => EffectVisualEffect(VFX_IMP_NEGATIVE_ENERGY, shouldMiss, 2.0f),
            EssenceType.Brimstone => EffectVisualEffect(VFX_IMP_FLAME_S, shouldMiss, 2.0f),
            EssenceType.Beshadowed => EffectVisualEffect(VFX_IMP_GREASE, shouldMiss, 2.0f),
            EssenceType.Binding => EffectVisualEffect(VFX_IMP_CHARM, shouldMiss, 2.0f),
            EssenceType.Bewitching => EffectVisualEffect(VFX_IMP_CHARM, shouldMiss, 2.0f),
            _ => EffectVisualEffect(VFX_IMP_MAGBLUE, shouldMiss, 2.0f)
        };

    private static IntPtr AoeEffectFromEssence(EssenceType essence) =>
        essence switch
        {
            EssenceType.NoEssence => EffectVisualEffect(76, FALSE, 13.0f),
            EssenceType.Frightful => EffectVisualEffect(261, FALSE, 6.0f),
            EssenceType.Draining => EffectVisualEffect(261, FALSE, 6.0f),
            EssenceType.Vitriolic => EffectVisualEffect(44, FALSE, 10.0f),
            EssenceType.Hindering => EffectVisualEffect(76, FALSE, 13.0f),
            EssenceType.Hellrime => EffectVisualEffect(62, FALSE, 4.0f),
            EssenceType.Utterdark => EffectVisualEffect(VFX_FNF_LOS_EVIL_20, FALSE, 1.5f),
            EssenceType.Brimstone => EffectVisualEffect(60, FALSE, 4.0f),
            EssenceType.Beshadowed => EffectVisualEffect(261, FALSE, 6.0f),
            EssenceType.Binding => EffectVisualEffect(261, FALSE, 6.0f),
            EssenceType.Bewitching => EffectVisualEffect(261, FALSE, 6.0f),
            _ => EffectVisualEffect(76, FALSE, 13.0f)
        };
}