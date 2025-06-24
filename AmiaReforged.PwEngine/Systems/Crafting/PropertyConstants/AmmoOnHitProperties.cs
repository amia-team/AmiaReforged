using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class AmmoOnHitProperties
{
    public static readonly CraftingCategory OnHits = new(categoryId: "ammo_onhits")
    {
        Label = "On Hit",
        Properties =
        [
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18, HitEffect.AbilityDrain(IPAbility.Charisma)),
                GuiLabel = "Ability Drain: Cha, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty =
                    ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18, HitEffect.AbilityDrain(IPAbility.Constitution)),
                GuiLabel = "Ability Drain: Con, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty =
                    ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18, HitEffect.AbilityDrain(IPAbility.Dexterity)),
                GuiLabel = "Ability Drain: Dex, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18, HitEffect.AbilityDrain(IPAbility.Strength)),
                GuiLabel = "Ability Drain: Str, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty =
                    ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18, HitEffect.AbilityDrain(IPAbility.Intelligence)),
                GuiLabel = "Ability Drain: Int, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18, HitEffect.AbilityDrain(IPAbility.Wisdom)),
                GuiLabel = "Ability Drain: Wis, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18,
                    HitEffect.Blindness(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Blindness, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC20,
                    HitEffect.Confusion(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Confusion, DC 20",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18,
                    HitEffect.Daze(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Daze, DC 20",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18,
                    HitEffect.Deafness(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Deafness, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18, HitEffect.Disease(DiseaseType.MummyRot)),
                GuiLabel = "Disease, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC20,
                    HitEffect.Fear(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Fear, DC 20",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18,
                    HitEffect.Hold(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Hold, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            // knock() dc20
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC20, HitEffect.Knock()),
                GuiLabel = "Knock: DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC16, HitEffect.LevelDrain()),
                GuiLabel = "Level Drain: DC 16",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC22,
                    HitEffect.ItemPoison(IPPoisonDamage.Constitution1d2)),
                GuiLabel = "Poison: Con 1d2 DC 20, 50%/2 rounds",
                PowerCost = 2,
                CraftingTier = CraftingTier.Divine
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18,
                    HitEffect.Silence(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Silence, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18,
                    HitEffect.Sleep(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Sleep, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18,
                    HitEffect.Slow(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Slow, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC18,
                    HitEffect.Stun(IPOnHitDuration.Duration50Pct2Rounds)),
                GuiLabel = "Stun, DC 18",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC20, HitEffect.Wounding(12)),
                GuiLabel = "Wounding, DC 20",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC20,
                    HitEffect.SlayRace(NwRace.FromRacialType(RacialType.Aberration)!)),
                GuiLabel = "Slay Aberration",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC20,
                    HitEffect.SlayRace(NwRace.FromRacialType(RacialType.Construct)!)),
                GuiLabel = "Slay Construct",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC20,
                    HitEffect.SlayRace(NwRace.FromRacialType(RacialType.Elemental)!)),
                GuiLabel = "Slay Elemental",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC20,
                    HitEffect.SlayRace(NwRace.FromRacialType(RacialType.MagicalBeast)!)),
                GuiLabel = "Slay Magical Beast",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = ItemProperty.OnHitEffect(IPOnHitSaveDC.DC20,
                    HitEffect.SlayRace(NwRace.FromRacialType(RacialType.Undead)!)),
                GuiLabel = "Slay Undead",
                PowerCost = 2,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = NWScript.ItemPropertyOnHitCastSpell(NWScript.IP_CONST_ONHIT_CASTSPELL_FREEZE, 10)!,
                GuiLabel = "Freeze (Slow 3 rounds,  CL 10)",
                PowerCost = 6,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty =
                    NWScript.ItemPropertyOnHitCastSpell(NWScript.IP_CONST_ONHIT_CASTSPELL_FLESH_TO_STONE, 10)!,
                GuiLabel = "Flesh to Stone (CL 10)",
                PowerCost = 6,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty = NWScript.ItemPropertyOnHitCastSpell(NWScript.IP_CONST_ONHIT_CASTSPELL_GREASE, 10)!,
                GuiLabel = "Grease (CL 8)",
                PowerCost = 6,
                CraftingTier = CraftingTier.Wondrous
            },
            new()
            {
                ItemProperty =
                    NWScript.ItemPropertyOnHitCastSpell(NWScript.IP_CONST_ONHIT_CASTSPELL_GREATER_DISPELLING, 10)!,
                GuiLabel = "Greater Dispelling (CL 20)",
                PowerCost = 6,
                CraftingTier = CraftingTier.Wondrous
            }
        ]
    };
}