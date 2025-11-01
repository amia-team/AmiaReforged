using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;

/// <summary>
/// Immutable definition for a property that can be rented or purchased by players.
/// </summary>
public sealed record RentablePropertyDefinition(
    PropertyId Id,
    string InternalName,
    SettlementTag Settlement,
    PropertyCategory Category,
    GoldAmount MonthlyRent,
    bool AllowsCoinhouseRental,
    bool AllowsDirectRental,
    CoinhouseTag? SettlementCoinhouseTag,
    GoldAmount? PurchasePrice,
    GoldAmount? MonthlyOwnershipTax,
    int EvictionGraceDays = 2)
{
    public PersonaId? DefaultOwner { get; init; }
}
