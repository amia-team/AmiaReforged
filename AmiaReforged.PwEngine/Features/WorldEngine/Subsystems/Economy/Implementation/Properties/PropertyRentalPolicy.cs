using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;

/// <summary>
/// Centralizes property rental rules so they can be tested outside of NWN concerns.
/// </summary>
[ServiceBinding(typeof(PropertyRentalPolicy))]
public sealed class PropertyRentalPolicy
{
    private readonly IRentablePropertyRepository _propertyRepository;
    private readonly IPropertyRentalLimitsProvider? _limitsProvider;
    private readonly PropertyRentalLimitsConfig? _staticLimitsConfig;

    public PropertyRentalPolicy(IRentablePropertyRepository propertyRepository, IPropertyRentalLimitsProvider limitsProvider)
    {
        _propertyRepository = propertyRepository;
        _limitsProvider = limitsProvider;
    }

    /// <summary>
    /// Constructor for testing with static configuration.
    /// </summary>
    public PropertyRentalPolicy(IRentablePropertyRepository propertyRepository, PropertyRentalLimitsConfig limitsConfig)
    {
        _propertyRepository = propertyRepository;
        _staticLimitsConfig = limitsConfig;
    }

    /// <summary>
    /// Constructor for testing with default configuration.
    /// </summary>
    public PropertyRentalPolicy(IRentablePropertyRepository propertyRepository)
        : this(propertyRepository, PropertyRentalLimitsConfig.Default)
    {
    }

    private PropertyRentalLimitsConfig GetLimitsConfig() =>
        _limitsProvider?.GetLimits() ?? _staticLimitsConfig ?? PropertyRentalLimitsConfig.Default;

    public async Task<RentalDecision> EvaluateAsync(RentPropertyRequest request, RentablePropertySnapshot property,
        PaymentCapabilitySnapshot capabilities, CancellationToken cancellationToken = default)
    {
        if (property.OccupancyStatus != PropertyOccupancyStatus.Vacant)
        {
            return RentalDecision.Denied(RentalDecisionReason.PropertyUnavailable,
                "The property is not currently available for rent.");
        }

        // Check if tenant already has reached the rental limit for this property category
        PropertyCategory category = property.Definition.Category;
        PropertyRentalLimitsConfig limitsConfig = GetLimitsConfig();
        int categoryLimit = limitsConfig.GetLimitForCategory(category);

        if (categoryLimit > 0)
        {
            List<RentablePropertySnapshot> existingRentalsInCategory =
                await _propertyRepository.GetPropertiesRentedByTenantInCategoryAsync(
                    request.Tenant, category, cancellationToken);

            if (existingRentalsInCategory.Count >= categoryLimit)
            {
                string categoryName = GetCategoryDisplayName(category);
                string message = categoryLimit == 1
                    ? $"You already have a {categoryName} property rented. You can only rent one {categoryName} property at a time."
                    : $"You have reached the maximum number of {categoryName} properties you can rent ({categoryLimit}).";

                return RentalDecision.Denied(RentalDecisionReason.TenantAlreadyHasActiveRentalInCategory, message);
            }
        }

        return request.PaymentMethod switch
        {
            RentalPaymentMethod.CoinhouseAccount => EvaluateCoinhouseRental(property, capabilities),
            RentalPaymentMethod.OutOfPocket => EvaluateDirectRental(property, capabilities),
            _ => RentalDecision.Denied(RentalDecisionReason.PaymentMethodNotAllowed, "Unsupported payment method.")
        };
    }

    private static string GetCategoryDisplayName(PropertyCategory category) => category switch
    {
        PropertyCategory.Residential => "residential",
        PropertyCategory.Commercial => "commercial",
        PropertyCategory.GuildHall => "guild hall",
        PropertyCategory.Industrial => "industrial",
        _ => "property"
    };

    /// <summary>
    /// Validates that a rent payment is allowed.
    /// Players can only pay rent during the month it's actually due or if it's overdue.
    /// </summary>
    public RentalDecision ValidateRentPayment(RentablePropertySnapshot property, DateOnly currentDate)
    {
        if (property.ActiveRental is null)
        {
            return RentalDecision.Denied(RentalDecisionReason.PropertyUnavailable,
                "This property has no active rental agreement.");
        }

        DateOnly dueDate = property.ActiveRental.NextPaymentDueDate;

        // Payment is allowed if:
        // 1. We're in the same month and year as the due date (payment month has arrived)
        // 2. We're past the due date (overdue - always allow catch-up payments)

        bool isInDueMonth = currentDate.Year == dueDate.Year && currentDate.Month == dueDate.Month;
        bool isOverdue = currentDate > dueDate;

        if (isInDueMonth || isOverdue)
        {
            return RentalDecision.Allowed();
        }

        // Payment is too early (trying to prepay for a future month)
        return RentalDecision.Denied(RentalDecisionReason.ExcessivePrepayment,
            $"Rent payment is not due yet. Your next payment is due on {dueDate:yyyy-MM-dd}.");
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
