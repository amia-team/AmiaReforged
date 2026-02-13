using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy;

/// <summary>
/// Maps faction letter codes to NWN standard faction constants.
/// Used by ./love and ./hate commands.
/// Ported from f_Love/f_Hate in mod_pla_cmd.nss.
/// </summary>
public static class FactionHelper
{
    /// <summary>
    /// Maps a single-letter faction code to the NWN STANDARD_FACTION constant.
    /// H = Hostile, C = Commoner, M = Merchant, D = Defender.
    /// Returns -1 if the code is unrecognized.
    /// </summary>
    public static int GetFactionId(string code)
    {
        return code.ToUpperInvariant() switch
        {
            "H" => NWScript.STANDARD_FACTION_HOSTILE,
            "C" => NWScript.STANDARD_FACTION_COMMONER,
            "M" => NWScript.STANDARD_FACTION_MERCHANT,
            "D" => NWScript.STANDARD_FACTION_DEFENDER,
            _ => -1
        };
    }

    /// <summary>
    /// Gets a human-readable name for a faction code.
    /// </summary>
    public static string GetFactionName(string code)
    {
        return code.ToUpperInvariant() switch
        {
            "H" => "Hostile",
            "C" => "Commoner",
            "M" => "Merchant",
            "D" => "Defender",
            _ => "Unknown"
        };
    }
}
