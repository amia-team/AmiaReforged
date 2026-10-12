using System.Numerics;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>The same preflight rules are used for invitations, answers and the final departure.</summary>
public sealed record PartyTravelEligibility(bool Online, bool LoginCharacter, bool InParty,
    string? Area, Vector3 Position, string? Aboard, bool Present, bool Authorized, bool Busy);

public static class ShipPartyTravelRules
{
    public static string? Exclusion(PartyTravelEligibility member, string? sourceShip, string departureArea,
        Vector3 anchor, float radius, bool disembarking)
    {
        if (!member.Online) return "Offline";
        if (!member.LoginCharacter) return "Use your own player character";
        if (!member.InParty) return "No longer in this party";
        if (member.Busy) return "Another transfer is in progress";
        if (member.Aboard != sourceShip || sourceShip != null && !member.Present) return "Not at the departure ship";
        if (!disembarking && (!string.Equals(member.Area, departureArea, StringComparison.OrdinalIgnoreCase) ||
            !float.IsFinite(radius) || radius <= 0 || !float.IsFinite(anchor.X) || !float.IsFinite(anchor.Y) || !float.IsFinite(anchor.Z) ||
            !float.IsFinite(member.Position.X) || !float.IsFinite(member.Position.Y) ||
            !float.IsFinite(member.Position.Z) || Vector3.Distance(member.Position, anchor) > radius)) return "Outside boarding range";
        if (!member.Authorized) return "Rental guest authorization required";
        return null;
    }
}
