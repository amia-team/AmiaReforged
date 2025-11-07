using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties.Commands;

/// <summary>
/// Handles rent payment requests by updating the rental agreement's next due date.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<PayRentCommand>))]
public sealed class PayRentCommandHandler : ICommandHandler<PayRentCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    private readonly IRentablePropertyRepository _propertyRepository;
    private readonly PropertyRentalPolicy _policy;

    public PayRentCommandHandler(
        IRentablePropertyRepository propertyRepository,
        PropertyRentalPolicy policy)
    {
        _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public async Task<CommandResult> HandleAsync(PayRentCommand command, CancellationToken cancellationToken = default)
    {
        RentablePropertySnapshot property = command.Property;

        if (property.ActiveRental is null)
        {
            return CommandResult.Fail("This property has no active rental agreement.");
        }

        if (!property.ActiveRental.Tenant.Equals(command.Tenant))
        {
            return CommandResult.Fail("You are not the tenant of this property.");
        }

        // Calculate the new due date (advance by one month from current due date)
        DateOnly currentDueDate = property.ActiveRental.NextPaymentDueDate;
        DateOnly newDueDate = _policy.CalculateNextDueDate(currentDueDate);

        Log.Info("Processing rent payment for property {PropertyId} ({InternalName}). " +
                 "Current due: {CurrentDue}, New due: {NewDue}, Payment method: {PaymentMethod}",
            property.Definition.Id,
            property.Definition.InternalName,
            currentDueDate,
            newDueDate,
            command.PaymentMethod);

        // Update the rental agreement with new due date
        RentalAgreementSnapshot updatedAgreement = property.ActiveRental with
        {
            NextPaymentDueDate = newDueDate
        };

        RentablePropertySnapshot updatedProperty = property with
        {
            ActiveRental = updatedAgreement
        };

        await _propertyRepository.PersistRentalAsync(updatedProperty, cancellationToken);

        Log.Info("Successfully processed rent payment for property {PropertyId} ({InternalName}). Next payment due: {NextDue}",
            property.Definition.Id,
            property.Definition.InternalName,
            newDueDate);

        return CommandResult.OkWith("nextDueDate", newDueDate);
    }
}
