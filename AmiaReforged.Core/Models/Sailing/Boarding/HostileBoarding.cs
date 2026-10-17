namespace AmiaReforged.Core.Models.Sailing;

public sealed record BoardingPoint(string Area, float X, float Y, float Z, float Facing = 0)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Area) && float.IsFinite(X) && float.IsFinite(Y) &&
        float.IsFinite(Z) && float.IsFinite(Facing) && X >= 0 && Y >= 0;
}

/// <summary>Written before departure; survives disconnects, failed jumps and server restarts.</summary>
public sealed record HostileBoardingVisit(string Actor, string OriginId, string? RentalId, string TargetId,
    BoardingPoint Origin, BoardingPoint Target, BoardingPoint Recovery,
    string? OriginCabinArea = null, string? TargetCabinArea = null);

public sealed record HostileBoardingFacts(bool SourceExists, bool TargetExists, bool Encounter,
    bool Aboard, bool Authorized, bool Busy, bool Deck, bool Spawn, bool Defenders,
    bool Recovery, float SourceSpeed = 0, float TargetSpeed = 0);

public sealed record HostileBoardingEligibility(bool Allowed, string Reason)
{
    public static HostileBoardingEligibility Denied(string reason) => new(false, reason);
    public static HostileBoardingEligibility Ready => new(true, "Ready to board hostile ship");
}

public enum BoardingDefenderState { Fighting, Defeated, Unavailable }
