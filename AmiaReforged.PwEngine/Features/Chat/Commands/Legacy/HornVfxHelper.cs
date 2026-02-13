using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy;

/// <summary>
/// Maps horn type names to VFX constants based on creature gender and appearance type.
/// Ported from GetHornVFX() in inc_td_appearanc.nss.
/// Gnomes (2), Halflings (3), and HalfOrcs (5) are not supported (no horn models).
/// </summary>
public static class HornVfxHelper
{
    /// <summary>
    /// Gets the VFX constant for horns of the given type on the specified creature.
    /// Returns -1 if the type or race is not supported.
    /// </summary>
    public static int GetHornVfx(NwCreature creature, string hornType)
    {
        int appearance = (int)creature.Appearance.RowIndex;
        int gender = (int)creature.Gender;
        hornType = hornType.ToLowerInvariant().Trim();

        // Gnome (2), Halfling (3), HalfOrc (5) don't have horn models
        if (appearance is 2 or 3 or 5)
            return -1;

        // HalfElf (6) uses Human values
        int raceKey = appearance == 6 ? 4 : appearance;

        return (hornType, raceKey, gender) switch
        {
            // Mephistopheles horns
            ("meph", 0, 0) => 858,  // Female Dwarf
            ("meph", 0, 1) => 830,  // Male Dwarf
            ("meph", 1, 0) => 802,  // Female Elf
            ("meph", 1, 1) => 773,  // Male Elf
            ("meph", 4, 0) => 745,  // Female Human/HalfElf
            ("meph", 4, 1) => 717,  // Male Human/HalfElf

            // Ox horns
            ("ox", 0, 0) => 859,
            ("ox", 0, 1) => 831,
            ("ox", 1, 0) => 803,
            ("ox", 1, 1) => 774,
            ("ox", 4, 0) => 746,
            ("ox", 4, 1) => 718,

            // Rothe horns
            ("rothe", 0, 0) => 860,
            ("rothe", 0, 1) => 832,
            ("rothe", 1, 0) => 804,
            ("rothe", 1, 1) => 775,
            ("rothe", 4, 0) => 747,
            ("rothe", 4, 1) => 719,

            // Balor horns
            ("balor", 0, 0) => 861,
            ("balor", 0, 1) => 833,
            ("balor", 1, 0) => 805,
            ("balor", 1, 1) => 776,
            ("balor", 4, 0) => 748,
            ("balor", 4, 1) => 720,

            // Antler horns
            ("antlers", 0, 0) => 862,
            ("antlers", 0, 1) => 834,
            ("antlers", 1, 0) => 806,
            ("antlers", 1, 1) => 777,
            ("antlers", 4, 0) => 749,
            ("antlers", 4, 1) => 721,

            // Dragon horns
            ("drag", 0, 0) => 863,
            ("drag", 0, 1) => 835,
            ("drag", 1, 0) => 807,
            ("drag", 1, 1) => 778,
            ("drag", 4, 0) => 750,
            ("drag", 4, 1) => 722,

            // Ram horns
            ("ram", 0, 0) => 864,
            ("ram", 0, 1) => 836,
            ("ram", 1, 0) => 808,
            ("ram", 1, 1) => 779,
            ("ram", 4, 0) => 751,
            ("ram", 4, 1) => 723,

            _ => -1
        };
    }

    /// <summary>
    /// Gets the list of available horn types for display in help text.
    /// </summary>
    public static string GetAvailableTypes()
    {
        return "meph, ox, rothe, balor, antlers, drag, ram";
    }
}
