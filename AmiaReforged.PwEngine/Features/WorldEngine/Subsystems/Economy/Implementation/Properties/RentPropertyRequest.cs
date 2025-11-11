using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;

/// <summary>
/// Data required to evaluate whether a persona may rent a property.
/// </summary>
public sealed record RentPropertyRequest(
    PersonaId Tenant,
    PropertyId PropertyId,
    RentalPaymentMethod PaymentMethod,
    DateOnly StartDate);
