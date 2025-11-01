using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;

/// <summary>
/// Current runtime state of a property including rental or ownership data.
/// </summary>
public sealed record RentablePropertySnapshot(
    RentablePropertyDefinition Definition,
    PropertyOccupancyStatus OccupancyStatus,
    PersonaId? CurrentTenant,
    PersonaId? CurrentOwner,
    RentalAgreementSnapshot? ActiveRental);
