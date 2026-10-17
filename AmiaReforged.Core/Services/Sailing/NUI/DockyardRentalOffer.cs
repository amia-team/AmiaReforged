using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>The per-row projection bound by the dockyard; unavailable configured offers remain visible.</summary>
public sealed record DockyardRentalOffer(string ShipId, string Name, string Specifications, string Terms, bool CanRent)
{
    // Short, single-line projections for labels. The complete original descriptions remain tooltips.
    public string DisplayName { get; init; } = "";
    public string Vessel { get; init; } = "";
    public string HullCargo { get; init; } = "";
    public string Fee { get; init; } = "";
    public string Deposit { get; init; } = "";
    public string Damage { get; init; } = "";
    public string Availability { get; init; } = "";
    public string Reason { get; init; } = "";
    public string Detail { get; init; } = "";
    public static DockyardRentalOffer Create(RentalVesselOption slot, ShipState? ship, bool checkoutEnabled,
        bool resourcesAvailable, bool hasOccupants)
    {
        bool available = checkoutEnabled && ship != null && ShipRentalService.Available(ship, slot.PortId) &&
            !ship.IsEconomyBusy && resourcesAvailable && !hasOccupants;
        string status = !checkoutEnabled ? "CHECKOUT DISABLED" : available ? "AVAILABLE FOR RENT" :
            ship == null ? "UNAVAILABLE — VESSEL NOT REGISTERED" :
            ship.IsInitializing ? "UNAVAILABLE — RESTORATION PENDING" :
            ship.Rental != null ? "UNAVAILABLE — RENTAL ALREADY RESERVED / ACTIVE" :
            !ship.IsStored || ship.StoredPortId != slot.PortId ? "UNAVAILABLE — NOT STORED HERE" :
            !resourcesAvailable ? "UNAVAILABLE — REQUIRED MODULE RESOURCES" : "UNAVAILABLE — VESSEL BUSY / INELIGIBLE";
        string specifications = ship == null ? $"Hull {slot.MaximumHull}; cargo {slot.CargoCapacity}; UNARMED; deck {slot.DeckAreaResRef}" :
            $"Hull {ship.Hull}/{ship.MaximumHull}; cargo {ship.CargoCapacity}; UNARMED; isolated deck {ship.DeckAreaResRef}";
        return new(slot.ShipId, $"{ship?.ShipName ?? slot.ShipName} — {slot.VesselClass}", specifications,
            $"{status}; fee {slot.Fee} gp + deposit {slot.Deposit} gp; damage {slot.DamageRate} gp/hull", available)
        {
            DisplayName = ship?.ShipName ?? slot.ShipName,
            Vessel = $"{slot.VesselClass} / UNARMED",
            HullCargo = ship == null ? $"Hull {slot.MaximumHull}; cargo {slot.CargoCapacity}" :
                $"Hull {ship.Hull}/{ship.MaximumHull}; cargo {ship.CargoCapacity}",
            Fee = $"Fee: {slot.Fee} gp", Deposit = $"Deposit: {slot.Deposit} gp", Damage = $"Damage: {slot.DamageRate} gp/hull",
            Availability = !checkoutEnabled ? "CHECKOUT DISABLED" : available ? "AVAILABLE FOR RENT" : "UNAVAILABLE",
            Reason = !checkoutEnabled ? "Rentals disabled or pending" : available ? "Ready for a new voyage." :
                ship == null ? "Vessel not registered" : ship.IsInitializing ? "Restoration pending" :
                ship.Rental != null ? "Reserved / active agreement" : !ship.IsStored || ship.StoredPortId != slot.PortId ? "Not stored at this port" :
                !resourcesAvailable ? "Module resources missing" : ship.IsDocking ? "Dock transfer pending" :
                ship.IsEconomyBusy ? "Vessel busy" : ship.IsDisabled ? "Hull disabled" :
                ship.Cargo.Any(c => c.Quantity > 0) ? "Cargo must be unloaded" : !ShipRentalService.Available(ship, slot.PortId) ? "Vessel ineligible" :
                hasOccupants ? "Vessel occupied" : "Vessel busy / ineligible",
            Detail = !checkoutEnabled ? "server/resource validation." : ""
        };
    }
}
