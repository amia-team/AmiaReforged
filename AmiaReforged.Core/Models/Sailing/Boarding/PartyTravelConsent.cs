namespace AmiaReforged.Core.Models.Sailing;

public enum PartyTravelAnswer { Pending, Accepted, Declined, Ineligible }

public sealed class PartyTravelMember(string player, string displayName, string? exclusion = null)
{
    public string Player { get; } = player;
    public string DisplayName { get; } = displayName;
    public PartyTravelAnswer Answer { get; internal set; } = exclusion == null ? PartyTravelAnswer.Pending : PartyTravelAnswer.Ineligible;
    public string? Exclusion { get; } = exclusion;
}

/// <summary>A captured roster: new party members never acquire consent through an old window.</summary>
public sealed class PartyTravelConsent(string initiator, IReadOnlyList<PartyTravelMember> members, DateTime expires)
{
    public string Initiator { get; } = initiator;
    public IReadOnlyList<PartyTravelMember> Members { get; } = Array.AsReadOnly(members.ToArray());
    public DateTime Expires { get; } = expires;
    public bool Started { get; private set; }
    public bool Cancelled { get; private set; }
    public bool IsOpen(DateTime now) => !Cancelled && (Started || now < Expires);
    public bool Answer(string player, bool accept, DateTime now)
    {
        var member = Members.SingleOrDefault(m => m.Player == player);
        if (Started || !IsOpen(now) || member?.Answer != PartyTravelAnswer.Pending || player == Initiator) return false;
        member.Answer = accept ? PartyTravelAnswer.Accepted : PartyTravelAnswer.Declined;
        return true;
    }
    public bool TryStart(bool withParty, DateTime now, Func<string, bool> eligible, out string[] participants)
    {
        participants = [];
        if (Started || !IsOpen(now) || !eligible(Initiator)) return false;
        if (withParty && Members.Any(m => m.Answer == PartyTravelAnswer.Pending)) return false;
        var selected = new[] { Initiator }.Concat(withParty
            ? Members.Where(m => m.Answer == PartyTravelAnswer.Accepted).Select(m => m.Player) : []).Distinct(StringComparer.Ordinal).ToArray();
        if (selected.Any(p => !eligible(p))) return false;
        Started = true; participants = selected; return true;
    }
    public void Cancel() => Cancelled = true;

    // Native window closure is presentation cleanup once confirmation has been
    // consumed. Disconnects and explicit cancellation still use Cancel().
    public bool CloseConfirmation()
    {
        if (Started) return false;
        Cancel();
        return true;
    }
}
