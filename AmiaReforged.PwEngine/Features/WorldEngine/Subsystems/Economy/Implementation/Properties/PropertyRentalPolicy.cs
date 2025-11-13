using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;

/// <summary>
/// Centralizes property rental rules so they can be tested outside of NWN concerns.
/// </summary>
[ServiceBinding(typeof(PropertyRentalPolicy))]
public sealed class PropertyRentalPolicy
{
    private readonly IRentablePropertyRepository _propertyRepository;

    public PropertyRentalPolicy(IRentablePropertyRepository propertyRepository)
    {
        _propertyRepository = propertyRepository;
    }

    public async Task<RentalDecision> EvaluateAsync(RentPropertyRequest request, RentablePropertySnapshot property,
        PaymentCapabilitySnapshot capabilities, CancellationToken cancellationToken = default)
    {
        if (property.OccupancyStatus != PropertyOccupancyStatus.Vacant)
        {
            return RentalDecision.Denied(RentalDecisionReason.PropertyUnavailable,
                "The property is not currently available for rent.");
        }

        // Check if tenant already has an active rental
        List<RentablePropertySnapshot> existingRentals =
            await _propertyRepository.GetPropertiesRentedByTenantAsync(request.Tenant, cancellationToken);

        if (existingRentals.Any())
        {
            return RentalDecision.Denied(RentalDecisionReason.TenantAlreadyHasActiveRental,
                "You already have an active rental. You can only rent one property at a time.");
        }

        return request.PaymentMethod switch
        {
            RentalPaymentMethod.CoinhouseAccount => EvaluateCoinhouseRental(property, capabilities),
            RentalPaymentMethod.OutOfPocket => EvaluateDirectRental(property, capabilities),
            _ => RentalDecision.Denied(RentalDecisionReason.PaymentMethodNotAllowed, "Unsupported payment method.")
        };
    }

    /// <summary>
    /// Validates that a rent payment won't result in excessive prepayment.
    /// Players can only pay up to 1 month in advance (current month + next month max).
    /// </summary>
    public RentalDecision ValidateRentPayment(RentablePropertySnapshot property, DateOnly currentDate)
    {
        if (property.ActiveRental is null)
        {
            return RentalDecision.Denied(RentalDecisionReason.PropertyUnavailable,
                "This property has no active rental agreement.");
        }

        // Calculate what the new due date would be after payment
        DateOnly nextDueDate = CalculateNextDueDate(property.ActiveRental.NextPaymentDueDate);

        // Check if payment would advance rent beyond 1 month from current date
        DateOnly maxAllowedDueDate = CalculateNextDueDate(currentDate);

        if (nextDueDate > maxAllowedDueDate)
        {
            return RentalDecision.Denied(RentalDecisionReason.ExcessivePrepayment,
                $"You can only pay rent up to 1 month in advance. Your next payment is not due until {property.ActiveRental.NextPaymentDueDate:yyyy-MM-dd}.");
        }

        return RentalDecision.Allowed();
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
