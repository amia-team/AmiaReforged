using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;

/// <summary>
/// Represents an active rental agreement between a persona and a property.
/// </summary>
public sealed record RentalAgreementSnapshot(
    PersonaId Tenant,
    DateOnly StartDate,
    DateOnly NextPaymentDueDate,
    GoldAmount MonthlyRent,
    RentalPaymentMethod PaymentMethod,
    DateTimeOffset? LastOccupantSeenUtc);
