namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;

/// <summary>
/// Reasons a rental request may be denied by the policy engine.
/// </summary>
public enum RentalDecisionReason
{
    None = 0,
    PropertyUnavailable,
    PaymentMethodNotAllowed,
    SettlementCoinhouseRequired,
    CoinhouseAccountRequired,
    InsufficientDirectFunds
}
