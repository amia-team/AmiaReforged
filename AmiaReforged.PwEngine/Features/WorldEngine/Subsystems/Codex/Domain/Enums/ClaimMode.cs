namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;

/// <summary>
/// Controls how many adventurers can claim a dynamic quest posting.
/// </summary>
public enum ClaimMode
{
    /// <summary>
    /// Any number of adventurers can claim the posting independently.
    /// </summary>
    Unlimited = 0,

    /// <summary>
    /// A finite number of claim slots are available, defined by the template's MaxClaimants.
    /// Once all slots are taken, others must wait for a slot to open.
    /// </summary>
    Limited = 1,

    /// <summary>
    /// Only a single adventurer can claim the posting. First come, first served.
    /// </summary>
    Exclusive = 2
}
