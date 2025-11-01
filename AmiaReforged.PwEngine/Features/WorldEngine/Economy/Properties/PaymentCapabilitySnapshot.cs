namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;

/// <summary>
/// Captures whether the renter can satisfy the requested payment method.
/// </summary>
public sealed record PaymentCapabilitySnapshot(
    bool HasSettlementCoinhouseAccount,
    bool HasSufficientDirectFunds);
