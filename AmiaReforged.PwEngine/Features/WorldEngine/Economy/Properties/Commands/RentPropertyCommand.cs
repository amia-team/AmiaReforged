using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties.Commands;

/// <summary>
/// Initiates a property rental attempt for a persona.
/// </summary>
public sealed record RentPropertyCommand(
    PersonaId Tenant,
    PropertyId PropertyId,
    RentalPaymentMethod PaymentMethod,
    DateOnly StartDate) : ICommand;
