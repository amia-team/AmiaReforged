using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties.Commands;

/// <summary>
/// Handles rental requests by delegating business decisions to the property rental policy.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<RentPropertyCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class RentPropertyCommandHandler : ICommandHandler<RentPropertyCommand>
{
    private readonly IRentablePropertyRepository _propertyRepository;
    private readonly IRentalPaymentCapabilityService _capabilityService;
    private readonly PropertyRentalPolicy _policy;

    public RentPropertyCommandHandler(
        IRentablePropertyRepository propertyRepository,
        IRentalPaymentCapabilityService capabilityService,
        PropertyRentalPolicy policy)
    {
        _propertyRepository = propertyRepository;
        _capabilityService = capabilityService;
        _policy = policy;
    }

    public async Task<CommandResult> HandleAsync(RentPropertyCommand command,
        CancellationToken cancellationToken = default)
    {
        RentablePropertySnapshot? property =
            await _propertyRepository.GetSnapshotAsync(command.PropertyId, cancellationToken);

        if (property is null)
        {
            return CommandResult.Fail("The requested property could not be found.");
        }

        RentPropertyRequest request = new(command.Tenant, command.PropertyId, command.PaymentMethod, command.StartDate);

        PaymentCapabilitySnapshot capabilities =
            await _capabilityService.EvaluateAsync(request, property, cancellationToken);

        RentalDecision decision = await _policy.EvaluateAsync(request, property, capabilities, cancellationToken);
        if (!decision.Success)
        {
            return CommandResult.Fail(decision.Message ?? Describe(decision.Reason));
        }

        DateOnly nextDueDate = _policy.CalculateNextDueDate(command.StartDate);
        RentalAgreementSnapshot agreement = new(
            command.Tenant,
            command.StartDate,
            nextDueDate,
            property.Definition.MonthlyRent,
            command.PaymentMethod,
            null);

        RentablePropertySnapshot updated = property with
        {
            OccupancyStatus = PropertyOccupancyStatus.Rented,
            CurrentTenant = command.Tenant,
            ActiveRental = agreement
        };

        await _propertyRepository.PersistRentalAsync(updated, cancellationToken);

        return CommandResult.OkWith("propertyId", updated.Definition.Id);
    }

    private static string Describe(RentalDecisionReason reason) => reason switch
    {
        RentalDecisionReason.PropertyUnavailable => "The property is already claimed.",
        RentalDecisionReason.PaymentMethodNotAllowed => "The chosen payment method is not supported for this property.",
        RentalDecisionReason.SettlementCoinhouseRequired =>
            "This property must be associated with a settlement coinhouse before bank-based rent can be configured.",
        RentalDecisionReason.CoinhouseAccountRequired =>
            "You need an active coinhouse account in this settlement to rent via bank account.",
        RentalDecisionReason.InsufficientDirectFunds =>
            "You do not have enough gold on hand to rent this property.",
        RentalDecisionReason.TenantAlreadyHasActiveRental =>
            "You already have an active rental. You can only rent one property at a time.",
        _ => "Unable to process the property rental request."
    };
}
