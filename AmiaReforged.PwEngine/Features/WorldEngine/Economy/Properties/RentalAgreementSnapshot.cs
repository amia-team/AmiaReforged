using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;

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
