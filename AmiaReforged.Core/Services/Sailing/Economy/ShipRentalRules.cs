using AmiaReforged.Core.Models.Sailing;
namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Native-free rental state transitions shared by runtime and regression tests.</summary>
public static class ShipRentalRules
{
    public static bool ResourcesValid(bool representation, bool deck, bool cabin, bool helm, bool exits, bool uniqueDeck) =>
        representation && deck && cabin && helm && exits && uniqueDeck;
    public static int Reserve(MaritimeEconomyState state, ShipState ship, ShipRentalRecord record)
    {
        if (!ShipRentalService.Available(ship, record.PortId) || record.ShipId != ship.ShipId ||
            state.Rentals.Any(r => r.ShipId == ship.ShipId && MaritimeMarketEngine.IsOpen(r.Status)))
            throw new InvalidOperationException("This vessel already has an agreement or is unavailable.");
        var reserved = MaritimeMarketEngine.Copy(record);
        reserved.VesselDefinitionId = ship.DefinitionId; reserved.DeckAreaResRef = ship.DeckAreaResRef; reserved.CabinAreaResRef = ship.CabinAreaResRef;
        var interiors = new[] { reserved.DeckAreaResRef, reserved.CabinAreaResRef }.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
        if (state.Rentals.Where(r => MaritimeMarketEngine.IsOpen(r.Status)).Any(r =>
            new[] { r.DeckAreaResRef, r.CabinAreaResRef }.Where(a => !string.IsNullOrWhiteSpace(a)).Intersect(interiors, StringComparer.OrdinalIgnoreCase).Any()))
            throw new InvalidOperationException("This cutter deck is already reserved by another agreement.");
        state.Rentals.Add(reserved); return 0;
    }
    public static int Activate(MaritimeEconomyState state, string rentalId, string account, DateTime now)
    {
        var lease = state.Rentals.Single(r => r.RentalId == rentalId);
        if (lease.Status != ShipRentalStatus.CheckoutPending || lease.RenterAccount != account)
            throw new InvalidOperationException("Rental already processed or unauthorized.");
        var vessel = state.Vessels[lease.ShipId];
        vessel.RentalStored = false; vessel.RentalStoredPort = null; vessel.RentalStorageRevision = checked(state.Revision + 1);
        lease.Status = ShipRentalStatus.Active; lease.StartedAt = now;
        return checked(-lease.Fee - lease.Deposit);
    }
    public static int Cancel(MaritimeEconomyState state, string rentalId, bool refund, DateTime now)
    {
        var lease = state.Rentals.Single(r => r.RentalId == rentalId);
        if (lease.Status == ShipRentalStatus.Cancelled) return 0;
        if (lease.Status != (refund ? ShipRentalStatus.Active : ShipRentalStatus.CheckoutPending))
            throw new InvalidOperationException("Payment state requires reconciliation.");
        var vessel = state.Vessels[lease.ShipId];
        vessel.RentalStored = true; vessel.RentalStoredPort = lease.PortId; vessel.RentalStorageRevision = checked(state.Revision + 1);
        lease.Status = ShipRentalStatus.Cancelled; lease.ReturnedAt = now;
        return refund ? checked(lease.Fee + lease.Deposit) : 0;
    }
    public static int RequireRecovery(MaritimeEconomyState state, string rentalId, int goldOwed, string reason)
    {
        var lease = state.Rentals.Single(r => r.RentalId == rentalId);
        if (!MaritimeMarketEngine.IsOpen(lease.Status) || goldOwed < 0) throw new InvalidOperationException("Invalid rental recovery transition.");
        lease.Status = ShipRentalStatus.RecoveryRequired; lease.GoldOwed = goldOwed; lease.RecoveryReason = reason;
        var vessel = state.Vessels[lease.ShipId]; vessel.RentalStored = true; vessel.RentalStoredPort = lease.PortId;
        vessel.RentalStorageRevision = checked(state.Revision + 1);
        return 0;
    }
    public static int Return(MaritimeEconomyState state, ShipState ship, string account, string port, bool hasAboard, DateTime now)
    {
        if (!ShipRentalService.CanReturn(ship, account, port, hasAboard))
            throw new InvalidOperationException("Rental return requires the renter, authorized port, stored empty ship and no occupants.");
        var lease = state.Rentals.Single(r => r.RentalId == ship.Rental!.RentalId);
        if (lease.Status != ShipRentalStatus.Active) throw new InvalidOperationException("Rental is no longer active.");
        var settlement = ShipRentalService.Settlement(lease, ship.Hull);
        var vessel = state.Vessels[lease.ShipId];
        vessel.RentalStored = true; vessel.RentalStoredPort = port; vessel.RentalStorageRevision = checked(state.Revision + 1);
        lease.ReturnPortId = port; lease.CargoAtReturn = 0;
        lease.ReturnHull = ship.Hull; lease.DamageSettlement = settlement.Damage; lease.DepositRefund = settlement.Refund;
        lease.Status = ShipRentalStatus.Returned; lease.ReturnedAt = now;
        return settlement.Refund;
    }
}
