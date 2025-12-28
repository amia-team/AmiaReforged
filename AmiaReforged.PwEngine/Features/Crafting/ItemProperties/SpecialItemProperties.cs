using AmiaReforged.PwEngine.Features.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Crafting.ItemProperties;

/// <summary>
/// Special zero-point properties that can only be added by DMs through the Mythal system.
/// These properties are tracked but do not count against item power limits.
/// </summary>
public static class SpecialItemProperties
{
    public static readonly CraftingCategory AdditionalProperties = new(categoryId: "additional_properties")
    {
        Label = "Additional Properties",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyAdditional(NWScript.IP_CONST_ADDITIONAL_CURSED)!,
                GuiLabel = "Additional Property (Cursed)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyAdditional(NWScript.IP_CONST_ADDITIONAL_UNKNOWN)!,
                GuiLabel = "Additional Property (Unknown)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            }
        ],
        BaseDifficulty = 0
    };

    public static readonly CraftingCategory AlignmentProperties = new(categoryId: "alignment_properties")
    {
        Label = "Alignment Properties",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, NWScript.IP_CONST_ALIGNMENT_CE)!,
                GuiLabel = "Use Limitation: Specific Alignment (CE)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, NWScript.IP_CONST_ALIGNMENT_CG)!,
                GuiLabel = "Use Limitation: Specific Alignment (CG)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, NWScript.IP_CONST_ALIGNMENT_CN)!,
                GuiLabel = "Use Limitation: Specific Alignment (CN)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, NWScript.IP_CONST_ALIGNMENT_LE)!,
                GuiLabel = "Use Limitation: Specific Alignment (LE)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, NWScript.IP_CONST_ALIGNMENT_LG)!,
                GuiLabel = "Use Limitation: Specific Alignment (LG)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, NWScript.IP_CONST_ALIGNMENT_LN)!,
                GuiLabel = "Use Limitation: Specific Alignment (LN)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, NWScript.IP_CONST_ALIGNMENT_NE)!,
                GuiLabel = "Use Limitation: Specific Alignment (NE)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, NWScript.IP_CONST_ALIGNMENT_NG)!,
                GuiLabel = "Use Limitation: Specific Alignment (NG)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, NWScript.IP_CONST_ALIGNMENT_TN)!,
                GuiLabel = "Use Limitation: Specific Alignment (TN)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            }
        ],
        BaseDifficulty = 0
    };

    public static readonly CraftingCategory UseLimitationAlignmentGroup = new(categoryId: "use_limit_align_group")
    {
        Label = "Use Limitation: Alignment Group",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByAlign(NWScript.IP_CONST_ALIGNMENTGROUP_CHAOTIC)!,
                GuiLabel = "Use Limitation: Alignment Group (Chaotic)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByAlign(NWScript.IP_CONST_ALIGNMENTGROUP_EVIL)!,
                GuiLabel = "Use Limitation: Alignment Group (Evil)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByAlign(NWScript.IP_CONST_ALIGNMENTGROUP_GOOD)!,
                GuiLabel = "Use Limitation: Alignment Group (Good)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByAlign(NWScript.IP_CONST_ALIGNMENTGROUP_LAWFUL)!,
                GuiLabel = "Use Limitation: Alignment Group (Lawful)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByAlign(NWScript.IP_CONST_ALIGNMENTGROUP_NEUTRAL)!,
                GuiLabel = "Use Limitation: Alignment Group (Neutral)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            }
        ],
        BaseDifficulty = 0
    };

    public static readonly CraftingCategory UseLimitationClass = new(categoryId: "use_limit_class")
    {
        Label = "Use Limitation: Class",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByClass(NWScript.IP_CONST_CLASS_BARBARIAN)!,
                GuiLabel = "Use Limitation: Class (Barbarian)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByClass(NWScript.IP_CONST_CLASS_BARD)!,
                GuiLabel = "Use Limitation: Class (Bard)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByClass(NWScript.IP_CONST_CLASS_CLERIC)!,
                GuiLabel = "Use Limitation: Class (Cleric)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByClass(NWScript.IP_CONST_CLASS_DRUID)!,
                GuiLabel = "Use Limitation: Class (Druid)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByClass(NWScript.IP_CONST_CLASS_FIGHTER)!,
                GuiLabel = "Use Limitation: Class (Fighter)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByClass(NWScript.IP_CONST_CLASS_MONK)!,
                GuiLabel = "Use Limitation: Class (Monk)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByClass(NWScript.IP_CONST_CLASS_PALADIN)!,
                GuiLabel = "Use Limitation: Class (Paladin)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByClass(NWScript.IP_CONST_CLASS_RANGER)!,
                GuiLabel = "Use Limitation: Class (Ranger)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByClass(NWScript.IP_CONST_CLASS_ROGUE)!,
                GuiLabel = "Use Limitation: Class (Rogue)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByClass(NWScript.IP_CONST_CLASS_SORCERER)!,
                GuiLabel = "Use Limitation: Class (Sorcerer)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByClass(NWScript.IP_CONST_CLASS_WIZARD)!,
                GuiLabel = "Use Limitation: Class (Wizard)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            }
        ],
        BaseDifficulty = 0
    };

    public static readonly CraftingCategory LightProperties = new(categoryId: "light_properties")
    {
        Label = "Light Properties",
        Properties =
        [
            // Dim (5m) - All colors
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_DIM, NWScript.IP_CONST_LIGHTCOLOR_BLUE)!,
                GuiLabel = "Light: Dim (5m), Blue",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_DIM, NWScript.IP_CONST_LIGHTCOLOR_GREEN)!,
                GuiLabel = "Light: Dim (5m), Green",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_DIM, NWScript.IP_CONST_LIGHTCOLOR_ORANGE)!,
                GuiLabel = "Light: Dim (5m), Orange",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_DIM, NWScript.IP_CONST_LIGHTCOLOR_PURPLE)!,
                GuiLabel = "Light: Dim (5m), Purple",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_DIM, NWScript.IP_CONST_LIGHTCOLOR_RED)!,
                GuiLabel = "Light: Dim (5m), Red",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_DIM, NWScript.IP_CONST_LIGHTCOLOR_WHITE)!,
                GuiLabel = "Light: Dim (5m), White",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_DIM, NWScript.IP_CONST_LIGHTCOLOR_YELLOW)!,
                GuiLabel = "Light: Dim (5m), Yellow",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            // Low (10m) - All colors
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_LOW, NWScript.IP_CONST_LIGHTCOLOR_BLUE)!,
                GuiLabel = "Light: Low (10m), Blue",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_LOW, NWScript.IP_CONST_LIGHTCOLOR_GREEN)!,
                GuiLabel = "Light: Low (10m), Green",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_LOW, NWScript.IP_CONST_LIGHTCOLOR_ORANGE)!,
                GuiLabel = "Light: Low (10m), Orange",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_LOW, NWScript.IP_CONST_LIGHTCOLOR_PURPLE)!,
                GuiLabel = "Light: Low (10m), Purple",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_LOW, NWScript.IP_CONST_LIGHTCOLOR_RED)!,
                GuiLabel = "Light: Low (10m), Red",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_LOW, NWScript.IP_CONST_LIGHTCOLOR_WHITE)!,
                GuiLabel = "Light: Low (10m), White",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_LOW, NWScript.IP_CONST_LIGHTCOLOR_YELLOW)!,
                GuiLabel = "Light: Low (10m), Yellow",
                GoldCost = 0,
                CraftingTier = CraftingTier.Minor,
                Removable = true
            },
            // Normal (15m) - All colors
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_NORMAL, NWScript.IP_CONST_LIGHTCOLOR_BLUE)!,
                GuiLabel = "Light: Normal (15m), Blue",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_NORMAL, NWScript.IP_CONST_LIGHTCOLOR_GREEN)!,
                GuiLabel = "Light: Normal (15m), Green",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_NORMAL, NWScript.IP_CONST_LIGHTCOLOR_ORANGE)!,
                GuiLabel = "Light: Normal (15m), Orange",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_NORMAL, NWScript.IP_CONST_LIGHTCOLOR_PURPLE)!,
                GuiLabel = "Light: Normal (15m), Purple",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_NORMAL, NWScript.IP_CONST_LIGHTCOLOR_RED)!,
                GuiLabel = "Light: Normal (15m), Red",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_NORMAL, NWScript.IP_CONST_LIGHTCOLOR_WHITE)!,
                GuiLabel = "Light: Normal (15m), White",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_NORMAL, NWScript.IP_CONST_LIGHTCOLOR_YELLOW)!,
                GuiLabel = "Light: Normal (15m), Yellow",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            },
            // Bright (20m) - All colors
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_BRIGHT, NWScript.IP_CONST_LIGHTCOLOR_BLUE)!,
                GuiLabel = "Light: Bright (20m), Blue",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_BRIGHT, NWScript.IP_CONST_LIGHTCOLOR_GREEN)!,
                GuiLabel = "Light: Bright (20m), Green",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_BRIGHT, NWScript.IP_CONST_LIGHTCOLOR_ORANGE)!,
                GuiLabel = "Light: Bright (20m), Orange",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_BRIGHT, NWScript.IP_CONST_LIGHTCOLOR_PURPLE)!,
                GuiLabel = "Light: Bright (20m), Purple",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_BRIGHT, NWScript.IP_CONST_LIGHTCOLOR_RED)!,
                GuiLabel = "Light: Bright (20m), Red",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_BRIGHT, NWScript.IP_CONST_LIGHTCOLOR_WHITE)!,
                GuiLabel = "Light: Bright (20m), White",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLight(NWScript.IP_CONST_LIGHTBRIGHTNESS_BRIGHT, NWScript.IP_CONST_LIGHTCOLOR_YELLOW)!,
                GuiLabel = "Light: Bright (20m), Yellow",
                GoldCost = 0,
                CraftingTier = CraftingTier.Lesser,
                Removable = true
            }
        ],
        BaseDifficulty = 0
    };

    public static readonly CraftingCategory QualityProperties = new(categoryId: "quality_properties")
    {
        Label = "Quality Properties",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE)!,
                GuiLabel = "Quality (Above Average)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_AVERAGE)!,
                GuiLabel = "Quality (Average)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_BELOW_AVERAGE)!,
                GuiLabel = "Quality (Below Average)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_CUT)!,
                GuiLabel = "Quality (Cut)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_DESTROYED)!,
                GuiLabel = "Quality (Destroyed)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_EXCELLENT)!,
                GuiLabel = "Quality (Excellent)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_GOD_LIKE)!,
                GuiLabel = "Quality (God-like)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_GOOD)!,
                GuiLabel = "Quality (Good)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_MASTERWORK)!,
                GuiLabel = "Quality (Masterwork)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_POLISHED)!,
                GuiLabel = "Quality (Polished)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_POOR)!,
                GuiLabel = "Quality (Poor)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_RAW)!,
                GuiLabel = "Quality (Raw)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_RUINED)!,
                GuiLabel = "Quality (Ruined)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_UNKOWN)!,
                GuiLabel = "Quality (Unknown)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_VERY_GOOD)!,
                GuiLabel = "Quality (Very Good)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_VERY_POOR)!,
                GuiLabel = "Quality (Very Poor)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            }
        ],
        BaseDifficulty = 0
    };

    public static readonly CraftingCategory UseLimitationRace = new(categoryId: "use_limit_race")
    {
        Label = "Use Limitation: Race",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_DWARF)!,
                GuiLabel = "Use Limitation: Race (Dwarf)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_ELF)!,
                GuiLabel = "Use Limitation: Race (Elf)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_GNOME)!,
                GuiLabel = "Use Limitation: Race (Gnome)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_HALFELF)!,
                GuiLabel = "Use Limitation: Race (Half-Elf)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_HALFLING)!,
                GuiLabel = "Use Limitation: Race (Halfling)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_HALFORC)!,
                GuiLabel = "Use Limitation: Race (Half-Orc)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_HUMAN)!,
                GuiLabel = "Use Limitation: Race (Human)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_HUMANOID_GOBLINOID)!,
                GuiLabel = "Use Limitation: Race (Goblinoid)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_HUMANOID_MONSTROUS)!,
                GuiLabel = "Use Limitation: Race (Monstrous Humanoid)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_HUMANOID_ORC)!,
                GuiLabel = "Use Limitation: Race (Orc)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_HUMANOID_REPTILIAN)!,
                GuiLabel = "Use Limitation: Race (Reptilian)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_OUTSIDER)!,
                GuiLabel = "Use Limitation: Race (Outsider)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_SHAPECHANGER)!,
                GuiLabel = "Use Limitation: Race (Shapechanger)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertyLimitUseByRace(NWScript.IP_CONST_RACIALTYPE_UNDEAD)!,
                GuiLabel = "Use Limitation: Race (Undead)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = false
            }
        ],
        BaseDifficulty = 0
    };
}


