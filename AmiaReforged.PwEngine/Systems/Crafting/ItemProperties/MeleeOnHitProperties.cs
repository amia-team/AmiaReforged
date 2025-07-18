using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.ItemProperties;

public static class MeleeOnHitProperties
{
    public static readonly CraftingCategory OnHits = new(categoryId: "on_hit_props")
    {
        // Divine DC 20
        Label = "On Hit",
        Properties =
        [
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Blindness(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Blindness: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Confusion(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Confusion: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Daze(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Daze: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Deafness(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Deafness: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22, HitEffect.Disease(DiseaseType.MummyRot)),
                GuiLabel = "Disease: Mummy Rot DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Doom(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Doom: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Fear(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Fear: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Hold(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Hold: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22, HitEffect.Knock()),
                GuiLabel = "Knockdown: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22, HitEffect.LevelDrain()),
                GuiLabel = "Level Drain: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.ItemPoison(IPPoisonDamage.Constitution1d2)),
                GuiLabel = "Poison: Con 1d2 DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Silence(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Silence: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Sleep(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Sleep: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Slow(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Slow: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Stun(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Stun: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22, HitEffect.Wounding(10)),
                GuiLabel = "Wounding: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            // Wonderforged, DC 22
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Blindness(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Blindness: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Confusion(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Confusion: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Daze(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Daze: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Deafness(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Deafness: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22, HitEffect.Disease(DiseaseType.MummyRot)),
                GuiLabel = "Disease: Mummy Rot DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Doom(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Doom: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Fear(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Fear: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Hold(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Hold: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22, HitEffect.Knock()),
                GuiLabel = "Knock: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22, HitEffect.LevelDrain()),
                GuiLabel = "Level Drain: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.ItemPoison(IPPoisonDamage.Constitution1d2)),
                GuiLabel = "Poison: Con 1d2 DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Silence(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Silence: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Sleep(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Sleep: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Slow(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Slow: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.Stun(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Stun: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22, HitEffect.Wounding(10)),
                GuiLabel = "Wounding: DC 22, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                ItemProperty = NWScript.ItemPropertyOnHitCastSpell(NWScript.IP_CONST_ONHIT_CASTSPELL_FREEZE, 10)!,
                GuiLabel = "Freeze (Slow 3 rounds,  CL 10)",
                PowerCost = 3,
                CraftingTier = CraftingTier.Divine
            }
        ]
    };
}