using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>The per-row projection bound by the dockyard; unavailable configured offers remain visible.</summary>
public sealed record DockyardRentalOffer(string ShipId, string Name, string Specifications, string Terms, bool CanRent)
{
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
            $"{status}; fee {slot.Fee} gp + deposit {slot.Deposit} gp; damage {slot.DamageRate} gp/hull", available);
    }
}
