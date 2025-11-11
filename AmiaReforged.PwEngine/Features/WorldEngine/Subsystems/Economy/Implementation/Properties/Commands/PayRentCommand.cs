using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties.Commands;

/// <summary>
/// Command to pay rent for a rented property.
/// </summary>
public sealed record PayRentCommand(
    RentablePropertySnapshot Property,
    PersonaId Tenant,
    RentalPaymentMethod PaymentMethod) : ICommand;
