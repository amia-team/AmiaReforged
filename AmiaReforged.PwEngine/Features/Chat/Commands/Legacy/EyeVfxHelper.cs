using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy;

/// <summary>
/// Maps eye color names to VFX constants based on creature gender and appearance type.
/// Ported from GetEyeVFX() in inc_td_appearanc.nss.
/// </summary>
public static class EyeVfxHelper
{
    // VFX constants for glowing eyes by race/gender/color
    // -1 means not available for that combination
    
    /// <summary>
    /// Gets the VFX constant for glowing eyes of the given color on the specified creature.
    /// Returns -1 if the color or race is not supported.
    /// </summary>
    public static int GetEyeVfx(NwCreature creature, string color)
    {
        int appearance = (int)creature.Appearance.RowIndex;
        int gender = (int)creature.Gender;
        color = color.ToLowerInvariant().Trim();

        // Special case: negred is only for Human and HalfElf
        if (color == "negred")
        {
            if (appearance is 4 or 6) // Human or HalfElf
                return 738;
            return -1;
        }

        // Map appearance type to race index: 0=Dwarf, 1=Elf, 2=Gnome, 3=Halfling, 4=Human, 5=HalfOrc, 6=HalfElf
        // VFX_EYES_* constants by color, gender, race
        // Female = 0, Male = 1
        return color switch
        {
            "cyan" => GetCyanEyeVfx(appearance, gender),
            "green" => GetGreenEyeVfx(appearance, gender),
            "orange" => GetOrangeEyeVfx(appearance, gender),
            "purple" => GetPurpleEyeVfx(appearance, gender),
            "red" => GetRedEyeVfx(appearance, gender),
            "white" => GetWhiteEyeVfx(appearance, gender),
            "yellow" => GetYellowEyeVfx(appearance, gender),
            "blue" => GetBlueEyeVfx(appearance, gender),
            _ => -1
        };
    }

    private static int GetCyanEyeVfx(int appearance, int gender)
    {
        // NWScript: VFX_EYES_CYN_*
        return (appearance, gender) switch
        {
            (0, 0) => NWScript.VFX_EYES_CYN_DWARF_FEMALE,
            (0, 1) => NWScript.VFX_EYES_CYN_DWARF_MALE,
            (1, 0) => NWScript.VFX_EYES_CYN_ELF_FEMALE,
            (1, 1) => NWScript.VFX_EYES_CYN_ELF_MALE,
            (2, 0) => NWScript.VFX_EYES_CYN_GNOME_FEMALE,
            (2, 1) => NWScript.VFX_EYES_CYN_GNOME_MALE,
            (3, 0) => NWScript.VFX_EYES_CYN_HALFLING_FEMALE,
            (3, 1) => NWScript.VFX_EYES_CYN_HALFLING_MALE,
            (4, 0) => NWScript.VFX_EYES_CYN_HUMAN_FEMALE,
            (4, 1) => NWScript.VFX_EYES_CYN_HUMAN_MALE,
            (5, 0) => NWScript.VFX_EYES_CYN_HALFORC_FEMALE,
            (5, 1) => NWScript.VFX_EYES_CYN_HALFORC_MALE,
            (6, 0) => NWScript.VFX_EYES_CYN_HUMAN_FEMALE, // HalfElf uses Human
            (6, 1) => NWScript.VFX_EYES_CYN_HUMAN_MALE,
            _ => -1
        };
    }

    private static int GetGreenEyeVfx(int appearance, int gender)
    {
        return (appearance, gender) switch
        {
            (0, 0) => NWScript.VFX_EYES_GREEN_DWARF_FEMALE,
            (0, 1) => NWScript.VFX_EYES_GREEN_DWARF_MALE,
            (1, 0) => NWScript.VFX_EYES_GREEN_ELF_FEMALE,
            (1, 1) => NWScript.VFX_EYES_GREEN_ELF_MALE,
            (2, 0) => NWScript.VFX_EYES_GREEN_GNOME_FEMALE,
            (2, 1) => NWScript.VFX_EYES_GREEN_GNOME_MALE,
            (3, 0) => NWScript.VFX_EYES_GREEN_HALFLING_FEMALE,
            (3, 1) => NWScript.VFX_EYES_GREEN_HALFLING_MALE,
            (4, 0) => NWScript.VFX_EYES_GREEN_HUMAN_FEMALE,
            (4, 1) => NWScript.VFX_EYES_GREEN_HUMAN_MALE,
            (5, 0) => NWScript.VFX_EYES_GREEN_HALFORC_FEMALE,
            (5, 1) => NWScript.VFX_EYES_GREEN_HALFORC_MALE,
            (6, 0) => NWScript.VFX_EYES_GREEN_HUMAN_FEMALE,
            (6, 1) => NWScript.VFX_EYES_GREEN_HUMAN_MALE,
            _ => -1
        };
    }

    private static int GetOrangeEyeVfx(int appearance, int gender)
    {
        return (appearance, gender) switch
        {
            (0, 0) => NWScript.VFX_EYES_ORG_DWARF_FEMALE,
            (0, 1) => NWScript.VFX_EYES_ORG_DWARF_MALE,
            (1, 0) => NWScript.VFX_EYES_ORG_ELF_FEMALE,
            (1, 1) => NWScript.VFX_EYES_ORG_ELF_MALE,
            (2, 0) => NWScript.VFX_EYES_ORG_GNOME_FEMALE,
            (2, 1) => NWScript.VFX_EYES_ORG_GNOME_MALE,
            (3, 0) => NWScript.VFX_EYES_ORG_HALFLING_FEMALE,
            (3, 1) => NWScript.VFX_EYES_ORG_HALFLING_MALE,
            (4, 0) => NWScript.VFX_EYES_ORG_HUMAN_FEMALE,
            (4, 1) => NWScript.VFX_EYES_ORG_HUMAN_MALE,
            (5, 0) => NWScript.VFX_EYES_ORG_HALFORC_FEMALE,
            (5, 1) => NWScript.VFX_EYES_ORG_HALFORC_MALE,
            (6, 0) => NWScript.VFX_EYES_ORG_HUMAN_FEMALE,
            (6, 1) => NWScript.VFX_EYES_ORG_HUMAN_MALE,
            _ => -1
        };
    }

    private static int GetPurpleEyeVfx(int appearance, int gender)
    {
        return (appearance, gender) switch
        {
            (0, 0) => NWScript.VFX_EYES_PUR_DWARF_FEMALE,
            (0, 1) => NWScript.VFX_EYES_PUR_DWARF_MALE,
            (1, 0) => NWScript.VFX_EYES_PUR_ELF_FEMALE,
            (1, 1) => NWScript.VFX_EYES_PUR_ELF_MALE,
            (2, 0) => NWScript.VFX_EYES_PUR_GNOME_FEMALE,
            (2, 1) => NWScript.VFX_EYES_PUR_GNOME_MALE,
            (3, 0) => NWScript.VFX_EYES_PUR_HALFLING_FEMALE,
            (3, 1) => NWScript.VFX_EYES_PUR_HALFLING_MALE,
            (4, 0) => NWScript.VFX_EYES_PUR_HUMAN_FEMALE,
            (4, 1) => NWScript.VFX_EYES_PUR_HUMAN_MALE,
            (5, 0) => NWScript.VFX_EYES_PUR_HALFORC_FEMALE,
            (5, 1) => NWScript.VFX_EYES_PUR_HALFORC_MALE,
            (6, 0) => NWScript.VFX_EYES_PUR_HUMAN_FEMALE,
            (6, 1) => NWScript.VFX_EYES_PUR_HUMAN_MALE,
            _ => -1
        };
    }

    private static int GetRedEyeVfx(int appearance, int gender)
    {
        return (appearance, gender) switch
        {
            (0, 0) => NWScript.VFX_EYES_RED_FLAME_DWARF_FEMALE,
            (0, 1) => NWScript.VFX_EYES_RED_FLAME_DWARF_MALE,
            (1, 0) => NWScript.VFX_EYES_RED_FLAME_ELF_FEMALE,
            (1, 1) => NWScript.VFX_EYES_RED_FLAME_ELF_MALE,
            (2, 0) => NWScript.VFX_EYES_RED_FLAME_GNOME_FEMALE,
            (2, 1) => NWScript.VFX_EYES_RED_FLAME_GNOME_MALE,
            (3, 0) => NWScript.VFX_EYES_RED_FLAME_HALFLING_FEMALE,
            (3, 1) => NWScript.VFX_EYES_RED_FLAME_HALFLING_MALE,
            (4, 0) => NWScript.VFX_EYES_RED_FLAME_HUMAN_FEMALE,
            (4, 1) => NWScript.VFX_EYES_RED_FLAME_HUMAN_MALE,
            (5, 0) => NWScript.VFX_EYES_RED_FLAME_HALFORC_FEMALE,
            (5, 1) => NWScript.VFX_EYES_RED_FLAME_HALFORC_MALE,
            (6, 0) => NWScript.VFX_EYES_RED_FLAME_HUMAN_FEMALE,
            (6, 1) => NWScript.VFX_EYES_RED_FLAME_HUMAN_MALE,
            _ => -1
        };
    }

    private static int GetWhiteEyeVfx(int appearance, int gender)
    {
        return (appearance, gender) switch
        {
            (0, 0) => NWScript.VFX_EYES_WHT_DWARF_FEMALE,
            (0, 1) => NWScript.VFX_EYES_WHT_DWARF_MALE,
            (1, 0) => NWScript.VFX_EYES_WHT_ELF_FEMALE,
            (1, 1) => NWScript.VFX_EYES_WHT_ELF_MALE,
            (2, 0) => NWScript.VFX_EYES_WHT_GNOME_FEMALE,
            (2, 1) => NWScript.VFX_EYES_WHT_GNOME_MALE,
            (3, 0) => NWScript.VFX_EYES_WHT_HALFLING_FEMALE,
            (3, 1) => NWScript.VFX_EYES_WHT_HALFLING_MALE,
            (4, 0) => NWScript.VFX_EYES_WHT_HUMAN_FEMALE,
            (4, 1) => NWScript.VFX_EYES_WHT_HUMAN_MALE,
            (5, 0) => NWScript.VFX_EYES_WHT_HALFORC_FEMALE,
            (5, 1) => NWScript.VFX_EYES_WHT_HALFORC_MALE,
            (6, 0) => NWScript.VFX_EYES_WHT_HUMAN_FEMALE,
            (6, 1) => NWScript.VFX_EYES_WHT_HUMAN_MALE,
            _ => -1
        };
    }

    private static int GetYellowEyeVfx(int appearance, int gender)
    {
        return (appearance, gender) switch
        {
            (0, 0) => NWScript.VFX_EYES_YEL_DWARF_FEMALE,
            (0, 1) => NWScript.VFX_EYES_YEL_DWARF_MALE,
            (1, 0) => NWScript.VFX_EYES_YEL_ELF_FEMALE,
            (1, 1) => NWScript.VFX_EYES_YEL_ELF_MALE,
            (2, 0) => NWScript.VFX_EYES_YEL_GNOME_FEMALE,
            (2, 1) => NWScript.VFX_EYES_YEL_GNOME_MALE,
            (3, 0) => NWScript.VFX_EYES_YEL_HALFLING_FEMALE,
            (3, 1) => NWScript.VFX_EYES_YEL_HALFLING_MALE,
            (4, 0) => NWScript.VFX_EYES_YEL_HUMAN_FEMALE,
            (4, 1) => NWScript.VFX_EYES_YEL_HUMAN_MALE,
            (5, 0) => NWScript.VFX_EYES_YEL_HALFORC_FEMALE,
            (5, 1) => NWScript.VFX_EYES_YEL_HALFORC_MALE,
            (6, 0) => NWScript.VFX_EYES_YEL_HUMAN_FEMALE,
            (6, 1) => NWScript.VFX_EYES_YEL_HUMAN_MALE,
            _ => -1
        };
    }

    private static int GetBlueEyeVfx(int appearance, int gender)
    {
        // Blue eyes use raw VFX IDs from the original NWScript
        return (appearance, gender) switch
        {
            (0, 0) => 329, // Female Dwarf
            (0, 1) => 328, // Male Dwarf
            (1, 0) => 331, // Female Elf
            (1, 1) => 330, // Male Elf
            (2, 0) => 333, // Female Gnome
            (2, 1) => 332, // Male Gnome
            (3, 0) => 327, // Female Halfling
            (3, 1) => 326, // Male Halfling
            (4, 0) => 325, // Female Human
            (4, 1) => 324, // Male Human
            (5, 0) => 335, // Female HalfOrc
            (5, 1) => 334, // Male HalfOrc
            (6, 0) => 325, // Female HalfElf (uses Human)
            (6, 1) => 324, // Male HalfElf (uses Human)
            _ => -1
        };
    }

    /// <summary>
    /// Gets the list of available eye colors for display in help text.
    /// </summary>
    public static string GetAvailableColors()
    {
        return "cyan, green, orange, purple, red, white, yellow, blue, negred (human/halfelf only)";
    }
}
