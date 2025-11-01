namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;

/// <summary>
/// Centralizes property rental rules so they can be tested outside of NWN concerns.
/// </summary>
public sealed class PropertyRentalPolicy
{
    public RentalDecision Evaluate(RentPropertyRequest request, RentablePropertySnapshot property,
        PaymentCapabilitySnapshot capabilities)
    {
        if (property.OccupancyStatus != PropertyOccupancyStatus.Vacant)
        {
            return RentalDecision.Denied(RentalDecisionReason.PropertyUnavailable,
                "The property is not currently available for rent.");
        }

        return request.PaymentMethod switch
        {
            RentalPaymentMethod.CoinhouseAccount => EvaluateCoinhouseRental(property, capabilities),
            RentalPaymentMethod.OutOfPocket => EvaluateDirectRental(property, capabilities),
            _ => RentalDecision.Denied(RentalDecisionReason.PaymentMethodNotAllowed, "Unsupported payment method.")
        };
    }

    public DateOnly CalculateNextDueDate(DateOnly startDate) => startDate.AddMonths(1);

    public bool IsEvictionEligible(RentalAgreementSnapshot agreement, RentablePropertyDefinition definition,
        DateTimeOffset evaluationTimeUtc, int? graceDaysOverride = null)
    {
    int graceDays = graceDaysOverride ?? definition.EvictionGraceDays;

        DateTimeOffset dueAtUtc = DateTime.SpecifyKind(
            agreement.NextPaymentDueDate.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

        if (evaluationTimeUtc <= dueAtUtc)
        {
            return false;
        }

        DateTimeOffset evictionThreshold = dueAtUtc.AddDays(graceDays);
        if (evaluationTimeUtc < evictionThreshold)
        {
            return false;
        }

        if (agreement.LastOccupantSeenUtc is null)
        {
            return evaluationTimeUtc >= evictionThreshold;
        }

        if (agreement.LastOccupantSeenUtc > dueAtUtc)
        {
            // The tenant was seen after the due date, so they are still considered active.
            return false;
        }

        return evaluationTimeUtc >= evictionThreshold;
    }

    private static RentalDecision EvaluateCoinhouseRental(RentablePropertySnapshot property,
        PaymentCapabilitySnapshot capabilities)
    {
        if (!property.Definition.AllowsCoinhouseRental)
        {
            return RentalDecision.Denied(RentalDecisionReason.PaymentMethodNotAllowed,
                "This property cannot be rented via coinhouse accounts.");
        }

        if (property.Definition.SettlementCoinhouseTag is null)
        {
            return RentalDecision.Denied(RentalDecisionReason.SettlementCoinhouseRequired,
                "A settlement coinhouse must be associated with this property before renting via bank account.");
        }

        if (!capabilities.HasSettlementCoinhouseAccount)
        {
            return RentalDecision.Denied(RentalDecisionReason.CoinhouseAccountRequired,
                "You need an active coinhouse account in this settlement to pay via bank.");
        }

        return RentalDecision.Allowed();
    }

    private static RentalDecision EvaluateDirectRental(RentablePropertySnapshot property,
        PaymentCapabilitySnapshot capabilities)
    {
        if (!property.Definition.AllowsDirectRental)
        {
            return RentalDecision.Denied(RentalDecisionReason.PaymentMethodNotAllowed,
                "This property cannot be rented with direct payments.");
        }

        if (!capabilities.HasSufficientDirectFunds)
        {
            return RentalDecision.Denied(RentalDecisionReason.InsufficientDirectFunds,
                "You do not have enough gold on hand to cover rent.");
        }

        return RentalDecision.Allowed();
    }
}
